using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
#if !NET
using Mono.Data.Sqlite;
#else
using Microsoft.Data.Sqlite;
#endif

namespace WindBot.Agent.Knowledge
{
    // Per-game card database. Built when the deck is known (before rock-paper-scissors),
    // from cards.cdb + card Lua scripts, then updated from engine events during the duel.
    // Contains no deck- or card-specific code: any deck builds the same way.

    public sealed class CardData
    {
        public int Id;
        public int Alias;
        public string Name;
        public string Text;
        public long Type;
        public long LevelRaw;
        public long Race;
        public long Attribute;
        public int Atk;
        public int Def;
        public long Setcode;
        public readonly string[] Strings = new string[16];
        public bool InDatabase;

        public int Level { get { return (int)(LevelRaw & 0xff); } }
    }

    public sealed class CardLink
    {
        /// <summary>condition / cost / effect / material</summary>
        public string Role;
        /// <summary>What happens to the linked cards, e.g. "덱에서 패에 넣기", "특수 소환", "엑시즈 소재".</summary>
        public string Relation;
        public string Location;
        public bool Opponent;
        /// <summary>No static condition (only runtime checks like IsAbleToGraveAsCost): any card qualifies, so it is not a meaningful link.</summary>
        public bool AnyCard;
        public FilterInfo Filter;
        public readonly List<int> Targets = new List<int>();
        public string SourceFunction;
        /// <summary>Cards picked at once (Lua's minimum, e.g. Ryzeal Cross ②: 2 from the GY).</summary>
        public int Count = 1;
    }

    public sealed class CountLimit
    {
        public int Count = 1;
        public string Key;
        public bool Oath;
        public bool PerDuel;
        public string Raw;
    }

    /// <summary>
    /// Protection from battle destruction read from Lua: EFFECT_INDESTRUCTABLE_BATTLE, or EFFECT_DESTROY_REPLACE
    /// (destroyed instead of... : once, at a cost such as a material).
    /// </summary>
    public sealed class BattleGuard
    {
        public bool Replace;
        /// <summary>EFFECT_TYPE_SINGLE: protects the card itself. Otherwise the holder's monsters that match Filter.</summary>
        public bool SelfOnly;
        /// <summary>Which of the holder's monsters (static predicates; null = any).</summary>
        public FilterInfo Filter;
        /// <summary>Works only while the holder has Xyz material (condition or replacement cost).</summary>
        public bool NeedsMaterial;

        public string Describe()
        {
            return (Replace ? "전투 파괴 대체" : "전투로 파괴되지 않음") + (SelfOnly ? " (자신)" : " (자기 쪽 몬스터" + (Filter != null && Filter.Raw != null ? ": " + Filter.Raw : "") + ")")
                   + (NeedsMaterial ? ", 소재가 있을 때" : "");
        }
    }

    public sealed class EffectEntry
    {
        public int Index;
        public int? DescIndex;
        public string Label;
        public string Kind;
        /// <summary>
        /// After this effect (its operation registers EFFECT_CANNOT_SPECIAL_SUMMON on us for the turn), the Extra Deck allows only
        /// Xyz monsters of this Rank (every main deck Ryzeal's rule summon: Rank 4); -1 no lock, -2 a lock we cannot read (nothing allowed).
        /// </summary>
        public int ExtraLockRank = -1; // AnyXyzRank (99): any Xyz monster
        /// <summary>After this effect only Xyz monsters of this Rank can declare attacks this turn (Ryzeal Plugin: Rank 4); -1 no lock.</summary>
        public int AttackLockRank = -1;
        public readonly List<string> Events = new List<string>();
        public readonly List<string> Categories = new List<string>();
        public string Range;
        public CountLimit Limit;
        /// <summary>Usage counter key: the count-limit key when there is one, otherwise unique to this card's effect.</summary>
        public string UsageKey;
        /// <summary>Xyz materials this effect removes from our field (cost or resolution). Requires that many materials.</summary>
        public int DetachCount;
        /// <summary>The resolution forbids normal summoning the added card (e.g. Lightning Crash: EFFECT_CANNOT_SUMMON).</summary>
        public bool BlocksNormalSummonOfAdded;
        /// <summary>Condition requires our field to be empty (Mulcharmy; Impermanence's hand activation).</summary>
        public bool RequiresEmptyField;
        /// <summary>The monster this effect summons has its effects negated (e.g. Node: "효과를 무효로 하고").</summary>
        public bool SummonsNegated;
        public readonly List<CardLink> Links = new List<CardLink>();
        public readonly List<string> Unresolved = new List<string>();
        public string Confidence = "inferred";
        /// <summary>Battle-destruction protection this effect gives (null if none).</summary>
        public BattleGuard Guard;
        /// <summary>What a negation effect can negate, read from Lua: "monster", "spelltrap" or "all"; null when the effect does not negate.</summary>
        public string NegateScope;
        /// <summary>What this interruption can respond to, read from its Lua condition (null when it is not a response effect).</summary>
        public ResponseFilter Response;
        /// <summary>Activated as a chain link (card activation, ignition, trigger, quick effect): something a response can answer.</summary>
        public bool Activates;
        /// <summary>Where the card is when this effect is activated (MZONE, SZONE, FZONE, HAND, GRAVE, REMOVED); empty = not known.</summary>
        public readonly List<string> SourceZones = new List<string>();
        /// <summary>What the resolution does, as CATEGORY@LOCATION (e.g. TOHAND@DECK, SPECIAL_SUMMON@GRAVE) plus bare categories.</summary>
        public readonly List<string> Moves = new List<string>();
        /// <summary>
        /// Does the resolution need the activating card still there (its operation checks the handler's IsRelateToEffect)?
        /// "all": the whole effect stops when the card leaves (Duo Drive / Detonator attaching from the GY);
        /// "part": only a part does (Impermanence's column negation); null: the effect resolves without the card (docs/CHAIN_REMOVAL.md).
        /// </summary>
        public string SourceStop;
        /// <summary>Which cards this removal can hit, read from its target / operation functions (null when it removes nothing on the field).</summary>
        public RemovalReach Reach;
        /// <summary>A standing protection this effect gives (cannot be targeted / destroyed by the opponent's card effects), or null.</summary>
        public CardShield Shield;
        /// <summary>The resolution makes its own card indestructible by card effects (Number F0 ⑤): a response that saves it from a destroy.</summary>
        public bool GuardsSelf;
        /// <summary>
        /// "Cannot activate the effects of monsters with N or more ATK" (EFFECT_CANNOT_ACTIVATE, Super Starslayer TY-PHON ①): N, or 0
        /// when the effect locks nothing by ATK. LockBoth: both players (target range 1,1), otherwise the opponent only.
        /// </summary>
        public int LockAtkAbove;
        public bool LockBoth;
        /// <summary>The resolution makes its controller draw each time the other player summons for the rest of the turn (Maxx "C", Mulcharmy).</summary>
        public DrawWatch DrawWatch;
    }

    /// <summary>
    /// "Draw 1 card each time your opponent summons" for the rest of the turn, read from the Lua of the activated effect
    /// (docs/DRAW_HANDTRAPS.md): continuous field effects on EVENT_SUMMON_SUCCESS / EVENT_SPSUMMON_SUCCESS that call Duel.Draw,
    /// the summon locations their condition checks (IsSummonLocation), and the End Phase shuffle back of hand cards above
    /// (the other player's field + N) that caps what the draws are worth (Mulcharmy).
    /// </summary>
    public sealed class DrawWatch
    {
        public string Card;
        public bool OnNormal;
        public bool OnSpecial;
        /// <summary>Summon locations that count (HAND, DECK, EXTRA, GRAVE...); empty = any.</summary>
        public readonly List<string> From = new List<string>();
        /// <summary>End Phase: hand cards beyond (the summoner's field + CapOffset) go back to the deck; -1 = no cap.</summary>
        public int CapOffset = -1;

        public bool Hits(bool special, string from)
        {
            if (special ? !OnSpecial : !OnNormal) return false;
            return From.Count == 0 || From.Contains(from);
        }

        public string Describe()
        {
            List<string> on = new List<string>();
            if (OnNormal) on.Add("일반 소환");
            if (OnSpecial) on.Add("특수 소환");
            return Card + ": 상대 " + String.Join("/", on.ToArray()) + (From.Count > 0 ? " (" + String.Join("·", From.ToArray()) + "에서)" : "") + "마다 1장 드로우"
                 + (CapOffset >= 0 ? ", 엔드 페이즈에 패가 (상대 필드 + " + CapOffset + ")장을 넘으면 덱으로" : "");
        }
    }

    /// <summary>
    /// A protection read from Lua (docs/CHAIN_REMOVAL.md 2): EFFECT_CANNOT_BE_EFFECT_TARGET or EFFECT_INDESTRUCTABLE_EFFECT,
    /// on the card itself (single) or on the other cards of its controller's field (field, target "c~=e:GetHandler()").
    /// Only unconditional ones with a filter we can read are kept.
    /// </summary>
    public sealed class CardShield
    {
        /// <summary>"target" (cannot be targeted) or "destroy" (cannot be destroyed).</summary>
        public string What;
        public bool Self;
        public bool Others;

        public string Describe()
        {
            return (What == "target" ? "효과 대상 불가" : "효과 파괴 불가") + " (" + (Self && Others ? "자신과 다른 카드" : Self ? "자신" : "자기 필드의 다른 카드") + ")";
        }
    }

    /// <summary>The cards a destroy / banish / bounce effect can take (docs/CHAIN_REMOVAL.md 2).</summary>
    public sealed class RemovalReach
    {
        /// <summary>Only the card whose activation it answers (Ghost Ogre: Duel.Destroy(eg)).</summary>
        public bool ChainCard;
        public bool FaceUpOnly;
        public bool SpellTrapOnly;
        public bool MonsterOnly;
        /// <summary>Only the opponent's cards (otherwise ours are legal too, e.g. Detonator ①).</summary>
        public bool OpponentOnly;
        /// <summary>Targets (EFFECT_FLAG_CARD_TARGET): a "cannot be targeted" protection stops it.</summary>
        public bool Targets;
        /// <summary>Destroys (rather than banishes / bounces): an "indestructible" protection stops it.</summary>
        public bool Destroys;

        public string Describe()
        {
            List<string> p = new List<string>();
            if (ChainCard) p.Add("발동한 그 카드");
            if (FaceUpOnly) p.Add("앞면");
            if (SpellTrapOnly) p.Add("마법·함정");
            if (MonsterOnly) p.Add("몬스터");
            p.Add(OpponentOnly ? "상대 카드만" : "양쪽");
            if (Targets) p.Add("대상 지정");
            if (Destroys) p.Add("파괴");
            return String.Join(", ", p.ToArray());
        }
    }

    /// <summary>How an opponent's response affects one of our effects.</summary>
    public enum HitKind { None, Negate, Disrupt }

    /// <summary>
    /// The kind of play an interruption can answer, read from its condition function:
    /// e.g. Ash Blossom = an effect that adds / summons / sends from the Deck; Ghost Ogre = an effect of a card on the field
    /// (monster effect, or a spell/trap effect that is not a card activation).
    /// </summary>
    public sealed class ResponseFilter
    {
        /// <summary>"chain" (responds to an activation), "solving" (applies while it resolves), "target" (picks a face-up card), "summons" (after N summons).</summary>
        public string Timing;
        /// <summary>"monster", "spelltrap" or "any".</summary>
        public string EffectType = "any";
        /// <summary>Spell/trap card activations are excluded (only their effects on the field).</summary>
        public bool NoCardActivation;
        /// <summary>Where the activating card must be (LOCATION_ names, e.g. ONFIELD; GRAVE|HAND|REMOVED); empty = anywhere.</summary>
        public readonly List<string> From = new List<string>();
        /// <summary>The answered effect must include one of these (CATEGORY name, "@LOCATION" when it must involve that location, e.g. TOHAND@DECK).</summary>
        public readonly List<string> Categories = new List<string>();
        /// <summary>Only an opponent's effect (rp==1-tp and similar).</summary>
        public bool OpponentOnly;
        /// <summary>Only a card with the name declared on activation (Crossout Designator).</summary>
        public bool DeclaredName;
        /// <summary>Summons the opponent must have made this turn (Nibiru: 5).</summary>
        public int SummonCount;
        /// <summary>Nothing in the condition narrowed it: treated as answering anything.</summary>
        public bool Open { get { return EffectType == "any" && !NoCardActivation && From.Count == 0 && Categories.Count == 0 && !DeclaredName && SummonCount == 0; } }
    }

    /// <summary>
    /// Xyz summon on top of one Xyz monster we control (Xyz.AddProcedure alterf, e.g. Full Armored Utopic Ray Lancer on a
    /// Rank 4 or lower Xyz): the monster and its materials become the new monster's materials.
    /// </summary>
    public sealed class OverlayProc
    {
        public int RankMax = 99;
        public int RankExact = -1;
        /// <summary>Hand cards the summon discards (one), or null when it discards nothing.</summary>
        public FilterInfo DiscardFilter;
        public readonly List<int> DiscardTargets = new List<int>();
        /// <summary>Once per turn (the op sets a flag with the card's id).</summary>
        public bool OncePerTurn;
        public string Text;
    }

    /// <summary>Battle-relevant numbers read from the Lua (used by the planner's breakthrough term).</summary>
    public sealed class BattleStats
    {
        /// <summary>ATK gained per Rank of the face-up Xyz monsters we control (F0: 500), 0 if none.</summary>
        public int AtkPerXyzRank;
        /// <summary>The Rank sum also counts Xyz monsters in the opponent's GY.</summary>
        public bool RanksInOpponentGrave;
        /// <summary>ATK change of every monster the opponent controls while this is face-up (Ray Lancer: -500).</summary>
        public int OpponentAtk;
        /// <summary>After destroying a monster by battle, attack again by detaching this many materials (Ray Lancer); -1 if it cannot.</summary>
        public int ChainAttackDetach = -1;
        /// <summary>At the start of the Battle Phase, negate the opponent's monsters ("attack": only attack position ones), or null.</summary>
        public string BattleStartNegate;
        public bool Any { get { return AtkPerXyzRank != 0 || OpponentAtk != 0 || ChainAttackDetach >= 0 || BattleStartNegate != null; } }
    }

    public sealed class CardEntry
    {
        public CardData Data;
        public int CopiesInDeck;
        public bool InMainDeck;
        public bool InExtraDeck;
        /// <summary>Only the opponent plays this card (a card both decks hold is ours, with OpponentCopies set).</summary>
        public bool Opponent;
        /// <summary>Copies in the opponent's declared deck list (0 when no list or not in it).</summary>
        public int OpponentCopies;
        public bool ScriptFound;
        public string ScriptPath;
        /// <summary>A trap that can be activated from the hand (Infinite Impermanence), and whether that needs our field empty.</summary>
        public bool TrapActInHand;
        public bool TrapActInHandNeedsEmptyField;
        /// <summary>While face-up, we cannot Xyz summon a monster with the same name as a card we control (Ryzeal Cross ①).</summary>
        public bool NoSameNameXyz;
        public readonly List<EffectEntry> Effects = new List<EffectEntry>();
        public CardLink Material;
        /// <summary>Xyz summon on top of one Xyz monster we control, or null.</summary>
        public OverlayProc Overlay;
        /// <summary>The Xyz materials are Xyz monsters (F0), and whether they must all have the same Rank.</summary>
        public bool XyzOfXyz;
        public bool SameRankMaterials;
        public readonly BattleStats Battle = new BattleStats();
        /// <summary>Incoming links: (card id, effect index or 0 for material, relation).</summary>
        public readonly List<KeyValuePair<int, string>> LinkedFrom = new List<KeyValuePair<int, string>>();
        public readonly List<string> Notes = new List<string>();
    }

    public sealed class KeyUsage
    {
        public int Activated;
        public int Used;
        public int ActivationNegated;
        public int EffectNegated;
        public int Resolved;
    }

    public sealed class GameCardDb
    {
        private readonly LuaConstants _constants;
        private readonly List<string> _scriptDirs;
        private readonly List<string> _databases;
        public readonly Dictionary<int, CardEntry> Cards = new Dictionary<int, CardEntry>();
        public readonly List<int> MainDeck = new List<int>();
        public readonly List<int> ExtraDeck = new List<int>();
        /// <summary>The opponent's deck list when the bot was told it (no list: empty, and only revealed cards are known).</summary>
        public readonly List<int> OpponentMainDeck = new List<int>();
        public readonly List<int> OpponentExtraDeck = new List<int>();
        public bool HasOpponentDeck { get { return OpponentMainDeck.Count + OpponentExtraDeck.Count > 0; } }

        // runtime: per player (0 self, 1 opponent), per count-limit key
        private readonly Dictionary<string, KeyUsage>[] _usage = { new Dictionary<string, KeyUsage>(), new Dictionary<string, KeyUsage>() };
        private readonly Dictionary<string, KeyUsage>[] _duelUsage = { new Dictionary<string, KeyUsage>(), new Dictionary<string, KeyUsage>() };
        private readonly Dictionary<int, PendingLink> _chain = new Dictionary<int, PendingLink>();
        public int Turn;
        public bool NormalSummonUsed;
        /// <summary>The opponent has Normal Summoned / Set this turn (MSG_SUMMONING by player 1).</summary>
        public bool OpponentNormalSummonUsed;
        public readonly List<string> TurnEvents = new List<string>();

        private sealed class PendingLink
        {
            public int Player;
            public CardEntry Card;
            public EffectEntry Effect;
            public bool ActivationNegated;
            public bool EffectNegated;
        }

        public GameCardDb(IEnumerable<string> databases, IEnumerable<string> scriptDirs, LuaConstants constants)
        {
            _databases = new List<string>(databases);
            _scriptDirs = new List<string>(scriptDirs);
            _constants = constants;
        }

        public int ConstantCount { get { return _constants.Count; } }
        public int ScriptDirCount { get { return _scriptDirs.Count(Directory.Exists); } }

        // ------------------------------------------------------------------ build

        public void BuildForDeck(IList<int> main, IList<int> extra)
        {
            MainDeck.Clear(); MainDeck.AddRange(main);
            ExtraDeck.Clear(); ExtraDeck.AddRange(extra);
            foreach (int id in main) AddCard(id, false).InMainDeck = true;
            foreach (int id in extra) AddCard(id, false).InExtraDeck = true;
            foreach (CardEntry c in Cards.Values)
                c.CopiesInDeck = main.Count(x => x == c.Data.Id) + extra.Count(x => x == c.Data.Id);
            ComputeLinks();
        }

        /// <summary>
        /// Adds the opponent's deck list (the engine never sends it; the bot is told it in its deck setting).
        /// Their cards get effects and links like ours, with links searched in their deck.
        /// </summary>
        public void AddOpponentDeck(IList<int> main, IList<int> extra)
        {
            OpponentMainDeck.Clear(); OpponentMainDeck.AddRange(main);
            OpponentExtraDeck.Clear(); OpponentExtraDeck.AddRange(extra);
            foreach (int id in main.Concat(extra)) AddCard(id, true);
            foreach (CardEntry c in Cards.Values)
                c.OpponentCopies = main.Count(x => x == c.Data.Id) + extra.Count(x => x == c.Data.Id);
            ComputeLinks();
        }

        /// <summary>Adds a card seen during the duel (opponent card revealed, token...). Links are computed against the known cards.</summary>
        public CardEntry EnsureCard(int id, bool opponent)
        {
            if (id == 0) return null;
            CardEntry existing;
            if (Cards.TryGetValue(id, out existing)) return existing;
            CardEntry c = AddCard(id, opponent);
            ComputeLinksFor(c);
            return c;
        }

        public CardEntry Get(int id)
        {
            CardEntry c;
            return Cards.TryGetValue(id, out c) ? c : null;
        }

        private CardEntry AddCard(int id, bool opponent)
        {
            CardEntry c;
            if (Cards.TryGetValue(id, out c)) return c;
            c = new CardEntry();
            c.Data = ReadCard(id);
            c.Opponent = opponent;
            Cards[id] = c;

            LuaScript script = LuaScript.Load(c.Data.Alias != 0 ? c.Data.Alias : id, _scriptDirs);
            if (!script.Found && c.Data.Alias != 0) script = LuaScript.Load(id, _scriptDirs);
            c.ScriptFound = script.Found;
            c.ScriptPath = script.Path;
            c.Notes.AddRange(script.Notes);
            if (script.Found) BuildEffects(c, script);
            else if ((c.Data.Type & Const("TYPE_NORMAL")) == 0) c.Notes.Add("script not found");
            return c;
        }

        private CardData ReadCard(int id)
        {
            CardData d = new CardData { Id = id, Name = "#" + id };
            foreach (string db in _databases)
            {
                if (!File.Exists(db)) continue;
                try
                {
                    using (SqliteConnection conn = new SqliteConnection("Data Source=" + db))
                    {
                        conn.Open();
                        using (IDbCommand cmd = new SqliteCommand(
                            "SELECT d.alias, d.setcode, d.type, d.level, d.race, d.attribute, d.atk, d.def, t.name, t.desc, " +
                            "t.str1,t.str2,t.str3,t.str4,t.str5,t.str6,t.str7,t.str8,t.str9,t.str10,t.str11,t.str12,t.str13,t.str14,t.str15,t.str16 " +
                            "FROM datas d JOIN texts t ON d.id = t.id WHERE d.id = " + id.ToString(CultureInfo.InvariantCulture), conn))
                        using (IDataReader r = cmd.ExecuteReader())
                        {
                            if (!r.Read()) continue;
                            d.Alias = (int)r.GetInt64(0);
                            d.Setcode = r.GetInt64(1);
                            d.Type = r.GetInt64(2);
                            d.LevelRaw = r.GetInt64(3);
                            d.Race = r.GetInt64(4);
                            d.Attribute = r.GetInt64(5);
                            d.Atk = (int)r.GetInt64(6);
                            d.Def = (int)r.GetInt64(7);
                            d.Name = r.IsDBNull(8) ? d.Name : r.GetString(8);
                            d.Text = r.IsDBNull(9) ? "" : r.GetString(9);
                            for (int i = 0; i < 16; ++i) d.Strings[i] = r.IsDBNull(10 + i) ? "" : r.GetString(10 + i);
                            d.InDatabase = true;
                            return d;
                        }
                    }
                }
                catch (Exception ex)
                {
                    d.Text = "cdb read failed: " + ex.Message;
                }
            }
            return d;
        }

        private long Const(string name)
        {
            long v;
            return _constants.TryGet(name, out v) ? v : 0;
        }

        private static List<string> Names(string expr, string prefix)
        {
            List<string> names = new List<string>();
            if (String.IsNullOrEmpty(expr)) return names;
            foreach (string part in expr.Split('|', '+'))
            {
                string t = part.Trim().Trim('(', ')');
                if (t.StartsWith(prefix)) names.Add(t.Substring(prefix.Length));
                else if (t.Length > 0) names.Add(t);
            }
            return names;
        }

        /// <summary>A function body plus the bodies of every s.* function it calls (one level, filters included).</summary>
        private static string DeepText(LuaScript s, string fn)
        {
            if (fn == null) return "";
            if (fn.StartsWith("(inline)")) return fn;
            string body = s.Body(fn);
            if (body == null) return "";
            StringBuilder sb = new StringBuilder(body);
            foreach (Match m in Regex.Matches(body, @"s\.(\w+)"))
            {
                string inner = s.Body(m.Groups[1].Value);
                if (inner != null && m.Groups[1].Value != fn) sb.AppendLine(inner);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Negation scope from the condition / target: monster effects only (re:IsMonsterEffect(), IsNegatableMonster, ...),
        /// spell / trap effects only (re:IsSpellTrapEffect(), ...), or all. Null when the effect does not negate
        /// (no NEGATE / DISABLE category and no Negate call in its operation).
        /// </summary>
        private static readonly Dictionary<string, string> CategoryNames = new Dictionary<string, string>
        {
            { "TOHAND", "패에 넣기" }, { "SEARCH", "서치" }, { "SPECIAL_SUMMON", "특수 소환" }, { "TOGRAVE", "묘지로 보내기" },
            { "DRAW", "드로우" }, { "DECKDES", "덱에서 묘지로" }, { "REMOVE", "제외" }, { "TODECK", "덱으로" }, { "TOEXTRA", "엑스트라 덱으로" }
        };
        private static readonly Dictionary<string, string> LocationNames = new Dictionary<string, string>
        {
            { "DECK", "덱" }, { "GRAVE", "묘지" }, { "HAND", "패" }, { "REMOVED", "제외 존" }, { "EXTRA", "엑스트라 덱" },
            { "ONFIELD", "필드" }, { "MZONE", "몬스터 존" }, { "SZONE", "마법·함정 존" }
        };

        public static string ResponseText(ResponseFilter f)
        {
            List<string> parts = new List<string>();
            parts.Add(f.Timing == "chain" ? "발동에 체인" : f.Timing == "solving" ? "처리 시 적용" : f.Timing == "summons" ? "상대 소환 " + f.SummonCount + "회 이후" : f.Timing == "declared" ? "이름 선언" : "필드의 앞면 카드 선택");
            if (f.OpponentOnly) parts.Add("상대 효과만");
            if (f.EffectType != "any") parts.Add(f.EffectType == "monster" ? "몬스터 효과" : "마법·함정 효과");
            if (f.NoCardActivation) parts.Add("몬스터 효과 또는 카드의 발동이 아닌 마법·함정 효과");
            if (f.From.Count > 0) parts.Add("발동 위치: " + String.Join("/", f.From.Select(x => LocationNames.ContainsKey(x) ? LocationNames[x] : x).ToArray()));
            if (f.Categories.Count > 0)
                parts.Add("포함 효과: " + String.Join(", ", f.Categories.Select(x =>
                {
                    string[] kv = x.Split('@');
                    string n = CategoryNames.ContainsKey(kv[0]) ? CategoryNames[kv[0]] : kv[0];
                    return kv.Length > 1 ? (LocationNames.ContainsKey(kv[1]) ? LocationNames[kv[1]] : kv[1]) + "에서 " + n : n;
                }).ToArray()));
            if (f.DeclaredName) parts.Add("선언한 이름의 카드만");
            if (f.Open) parts.Add(f.OpponentOnly ? "그 외 조건 없음" : "조건 없음(모든 효과)");
            return String.Join(" / ", parts.ToArray());
        }

        /// <summary>
        /// "all" when the operation returns early unless the activating card is still related to the effect
        /// (if not c:IsRelateToEffect(e) then return end), "part" when only a branch checks it, null when it never does.
        /// c is any local bound to e:GetHandler().
        /// </summary>
        private static string ReadSourceStop(EffectDef def, LuaScript s)
        {
            if (def.Operation == null) return null;
            string op = def.Operation.StartsWith("(inline)") ? def.Operation : s.Body(def.Operation);
            if (op == null) return null;
            List<string> names = new List<string> { Regex.Escape("e:GetHandler()") };
            foreach (Match m in Regex.Matches(op, @"local\s+(\w+)\s*=\s*e:GetHandler\(\)")) names.Add(m.Groups[1].Value);
            string who = "(?:" + String.Join("|", names.ToArray()) + ")";
            if (Regex.IsMatch(op, @"if\s+not\s+" + who + @":IsRelateToEffect\(e\)\s+then\s+return\b")) return "all";
            if (Regex.IsMatch(op, @"(?<![\w.])" + who + @":IsRelateToEffect\(e\)")) return "part";
            return null;
        }

        /// <summary>
        /// Reach of a removal (destroy / banish / bounce to hand or deck) from its Lua: the chkc test of the target function
        /// (IsFaceup, IsSpellTrap, IsMonster, IsControler(1-tp)), or the answered card itself when the operation removes eg / re's handler.
        /// </summary>
        private static RemovalReach ReadRemovalReach(EffectEntry e, EffectDef def, LuaScript s)
        {
            bool removes = e.Categories.Contains("DESTROY") || e.Categories.Contains("REMOVE") || e.Categories.Contains("TOHAND") || e.Categories.Contains("TODECK");
            if (!removes || e.Categories.Contains("SEARCH")) return null;
            RemovalReach r = new RemovalReach();
            r.Targets = def.PropertyExpr != null && def.PropertyExpr.Contains("EFFECT_FLAG_CARD_TARGET");
            r.Destroys = e.Categories.Contains("DESTROY");
            string op = DeepText(s, def.Operation);
            if (e.Events.Any(x => x == "CHAINING") && Regex.IsMatch(op, @"Duel\.(Destroy|Remove|SendtoHand|SendtoDeck)\(\s*(eg|re:GetHandler\(\))"))
            {
                r.ChainCard = true;
                r.OpponentOnly = true;
                return r;
            }
            string tg = def.Target == null ? "" : def.Target.StartsWith("(inline)") ? def.Target : (s.Body(def.Target) ?? "");
            Match chk = Regex.Match(tg, @"if\s+chkc\s+then\s+return\s+([^\n]*)");
            string test = chk.Success ? chk.Groups[1].Value : "";
            if (!chk.Success)
            {
                // No chkc: the filter passed to SelectTarget / SelectMatchingCard and its locations.
                Match sel = Regex.Match(tg + DeepText(s, def.Operation), @"Select(?:Target|MatchingCard)\(\s*tp\s*,\s*([^,]+),\s*tp\s*,\s*([^,]+),\s*([^,]+),");
                if (!sel.Success) return null;
                test = sel.Groups[1].Value + " " + (s.Body(sel.Groups[1].Value.Replace("s.", "").Trim()) ?? "");
                if (sel.Groups[2].Value.Trim() == "0") r.OpponentOnly = true;
            }
            if (test.Contains("IsFaceup")) r.FaceUpOnly = true;
            if (test.Contains("IsSpellTrap") || test.Contains("TYPE_SPELL+TYPE_TRAP") || test.Contains("TYPE_SPELL|TYPE_TRAP")) r.SpellTrapOnly = true;
            else if (test.Contains("IsMonster") || test.Contains("IsLocation(LOCATION_MZONE)")) r.MonsterOnly = true;
            if (test.Contains("IsControler(1-tp)")) r.OpponentOnly = true;
            return r;
        }

        /// <summary>
        /// A standing "cannot be targeted / destroyed by card effects" protection: single on the card itself, or field over its
        /// controller's side with the target "c~=e:GetHandler()" (Number F0 ④: every other card on the field). Conditional ones and
        /// filters we cannot read (e.g. "Traptrix" monsters only) are skipped and noted on the card.
        /// </summary>
        private static CardShield ReadShield(EffectDef def, LuaScript s, CardEntry c)
        {
            string code = def.CodeExpr ?? "";
            string what = code.Contains("EFFECT_CANNOT_BE_EFFECT_TARGET") ? "target" : code.Contains("EFFECT_INDESTRUCTABLE_EFFECT") ? "destroy" : null;
            if (what == null) return null;
            List<string> type = Names(def.TypeExpr, "EFFECT_TYPE_");
            if (def.Condition != null) { c.Notes.Add("조건부 보호 미반영: " + code); return null; }
            if (type.Contains("SINGLE")) return new CardShield { What = what, Self = true };
            if (!type.Contains("FIELD") || def.TargetRangeExpr == null) return null;
            string own = def.TargetRangeExpr.Split(',')[0].Trim();
            if (own == "0") return null;
            string tg = DeepText(s, def.Target).Replace("(inline)", "").Trim();
            if (Regex.IsMatch(tg, @"^function\s*\(\s*e\s*,\s*c\s*\)\s*return\s+c\s*~=\s*e:GetHandler\(\)\s*end\)?$"))
                return new CardShield { What = what, Others = true };
            c.Notes.Add("보호 대상 필터 미해석: " + code + " " + tg);
            return null;
        }

        /// <summary>DrawWatch from the operation and the functions it registers (two levels deep), or null.</summary>
        private static DrawWatch ReadDrawWatch(CardEntry c, EffectDef def, LuaScript s)
        {
            if (def.Operation == null || def.Operation.StartsWith("(inline)")) return null;
            string op = s.Body(def.Operation) ?? "";
            if (!op.Contains("Duel.RegisterEffect")) return null;
            bool normal = op.Contains("EVENT_SUMMON_SUCCESS"), special = op.Contains("EVENT_SPSUMMON_SUCCESS");
            if (!normal && !special) return null;
            StringBuilder all = new StringBuilder(op);
            HashSet<string> seen = new HashSet<string> { def.Operation };
            List<string> frontier = new List<string> { op };
            for (int depth = 0; depth < 2; ++depth)
            {
                List<string> next = new List<string>();
                foreach (string text in frontier)
                    foreach (Match m in Regex.Matches(text, @"s\.(\w+)"))
                    {
                        if (!seen.Add(m.Groups[1].Value)) continue;
                        string body = s.Body(m.Groups[1].Value);
                        if (body == null) continue;
                        all.AppendLine(body);
                        next.Add(body);
                    }
                frontier = next;
            }
            string t = all.ToString();
            if (!Regex.IsMatch(t, @"Duel\.Draw\(\s*tp\b")) return null;
            DrawWatch w = new DrawWatch { Card = c.Data.Name, OnNormal = normal, OnSpecial = special };
            Match loc = Regex.Match(t, @"IsSummonLocation\(([^)]*)\)");
            if (loc.Success) w.From.AddRange(Names(loc.Groups[1].Value, "LOCATION_"));
            Match cap = Regex.Match(t, @"GetFieldGroupCount\(\s*tp\s*,\s*0\s*,\s*LOCATION_ONFIELD\s*\)\s*\+\s*(\d+)");
            if (cap.Success && t.Contains("SendtoDeck")) w.CapOffset = int.Parse(cap.Groups[1].Value, CultureInfo.InvariantCulture);
            return w;
        }

        private static string ReadNegateScope(EffectEntry e, EffectDef def, LuaScript s)
        {
            string op = DeepText(s, def.Operation);
            bool negates = e.Categories.Contains("NEGATE") || e.Categories.Contains("DISABLE")
                        || op.Contains("NegateEffect(") || op.Contains("NegateActivation(") || op.Contains("NegateRelatedChain(");
            if (!negates) return null;
            string t = DeepText(s, def.Condition) + " " + DeepText(s, def.Target);
            bool monster = t.Contains("IsMonsterEffect()") || t.Contains("IsActiveType(TYPE_MONSTER)") || t.Contains("IsNegatableMonster");
            bool spellTrap = t.Contains("IsSpellTrapEffect()") || t.Contains("IsSpellEffect()") || t.Contains("IsTrapEffect()")
                          || t.Contains("IsActiveType(TYPE_SPELL+TYPE_TRAP)") || t.Contains("IsActiveType(TYPE_SPELL|TYPE_TRAP)");
            if (monster && !spellTrap) return "monster";
            if (spellTrap && !monster) return "spelltrap";
            return "all";
        }

        private static readonly Regex CategoryCheck = new Regex(@"(?:check\w*|GetOperationInfo|GetPossibleOperationInfo|IsHasCategory)\(\s*(?:ev\s*,\s*)?CATEGORY_(\w+)(?:\s*,\s*(true|false))?");
        private static readonly Regex CheckedLocation = new Regex(@"&\s*LOCATION_(DECK|GRAVE|HAND|REMOVED|EXTRA)\b");
        private static readonly Regex TriggeringLocation = new Regex(@"==\s*LOCATION_(\w+)");
        private static readonly Regex SummonCountCheck = new Regex(@"GetFlagEffect\(\s*1-tp\s*,\s*id\s*\)\s*>=\s*(\d+)");

        /// <summary>
        /// The response condition of an interruption: answers a chain (EVENT_CHAINING / CHAIN_SOLVING), negates, or fires after
        /// the opponent's Nth summon. Read from the condition (and the target for picked cards); null for other effects.
        /// </summary>
        private static ResponseFilter ReadResponse(EffectEntry e, EffectDef def, LuaScript s)
        {
            bool chain = e.Events.Contains("CHAINING"), solving = e.Events.Contains("CHAIN_SOLVING");
            string cond = DeepText(s, def.Condition);
            Match count = SummonCountCheck.Match(cond);
            if (!chain && !solving && e.NegateScope == null && !count.Success) return null;
            ResponseFilter f = new ResponseFilter();
            f.Timing = chain ? "chain" : solving ? "solving" : count.Success ? "summons" : "target";
            if (count.Success) f.SummonCount = int.Parse(count.Groups[1].Value);

            string t = f.Timing == "target" ? cond + " " + DeepText(s, def.Target) : cond;
            bool monster = t.Contains("IsMonsterEffect()") || t.Contains("IsActiveType(TYPE_MONSTER)") || t.Contains("IsNegatableMonster");
            bool spellTrap = t.Contains("IsSpellTrapEffect()") || t.Contains("IsSpellEffect()") || t.Contains("IsTrapEffect()")
                          || t.Contains("IsActiveType(TYPE_SPELL+TYPE_TRAP)") || t.Contains("IsActiveType(TYPE_SPELL|TYPE_TRAP)");
            f.NoCardActivation = t.Contains("not re:IsHasType(EFFECT_TYPE_ACTIVATE)");
            // "monster effect, or a spell/trap effect that is not a card activation" (Ghost Ogre): any effect type, activations excluded.
            f.EffectType = f.NoCardActivation ? "any" : monster && !spellTrap ? "monster" : spellTrap && !monster ? "spelltrap" : "any";
            if (f.Timing == "target") f.From.Add("ONFIELD"); // a picked card is on the field

            if (t.Contains("GetHandler():IsOnField()") && !f.From.Contains("ONFIELD")) f.From.Add("ONFIELD");
            if (t.Contains("CHAININFO_TRIGGERING_LOCATION"))
                foreach (Match m in TriggeringLocation.Matches(t))
                    if (!f.From.Contains(m.Groups[1].Value)) f.From.Add(m.Groups[1].Value);

            // Categories the answered effect must include: check(CATEGORY_X[, true]) with the checked location (&LOCATION_DECK).
            Match loc = CheckedLocation.Match(t);
            foreach (Match m in CategoryCheck.Matches(t))
            {
                string cat = m.Groups[1].Value;
                bool located = loc.Success && m.Groups[2].Value != "false" && cat != "DRAW" && cat != "DECKDES" && cat != "SEARCH";
                string key = located ? cat + "@" + loc.Groups[1].Value : cat;
                if (!f.Categories.Contains(key)) f.Categories.Add(key);
            }

            f.OpponentOnly = Regex.IsMatch(cond, @"\b(rp|ep)\s*==\s*1-tp\b|\b(rp|ep)\s*~=\s*tp\b");
            f.DeclaredName = DeepText(s, def.Operation).Contains("IsOriginalCodeRule(e:GetLabel())");
            if (f.DeclaredName)
            {
                // Declares a name and stops that card wherever it is activated (Crossout Designator): not a field pick.
                f.Timing = "declared";
                f.From.Clear();
                f.EffectType = "any";
            }
            return f;
        }

        /// <summary>Relation text of an effect link → the category the engine's operation info carries.</summary>
        private static string MoveCategory(string relation)
        {
            if (relation.Contains("엑시즈 소재로") || relation.Contains("세트")) return null; // Overlay / SSet: no category
            if (relation.Contains("특수 소환")) return "SPECIAL_SUMMON";
            if (relation.Contains("패에 넣기")) return "TOHAND";
            if (relation.Contains("묘지로")) return "TOGRAVE";
            if (relation.Contains("덱으로")) return "TODECK";
            if (relation.Contains("제외")) return "REMOVE";
            if (relation.Contains("파괴")) return "DESTROY";
            return null;
        }

        /// <summary>Activation zone, chain-ness and resolution moves of an effect: what an opponent's response checks.</summary>
        private void ReadProfile(CardEntry c, EffectEntry e)
        {
            e.Activates = e.Kind == "카드의 발동" || e.Kind == "기동 효과" || e.Kind == "유발 즉시 효과" || e.Kind.StartsWith("유발 효과");
            if (e.Kind == "카드의 발동") e.SourceZones.Add(IsMonster(c.Data) ? "HAND" : IsType(c.Data, "TYPE_FIELD") ? "FZONE" : "SZONE");
            else if (e.Range != null) e.SourceZones.AddRange(e.Range.Split('|').Where(x => x.Length > 0));
            else if (e.Events.Any(x => x.Contains("SUMMON_SUCCESS") || x == "FLIP")) e.SourceZones.Add("MZONE");
            else if (e.Events.Any(x => x.Contains("TO_GRAVE"))) e.SourceZones.Add("GRAVE");
            else if (e.Events.Any(x => x == "REMOVE")) e.SourceZones.Add("REMOVED");
            else if (e.Events.Any(x => x == "TO_HAND")) e.SourceZones.Add("HAND");
            foreach (string cat in e.Categories) if (!e.Moves.Contains(cat)) e.Moves.Add(cat);
            foreach (CardLink l in e.Links)
            {
                if (l.Role != "effect" || l.Relation.StartsWith("코스트")) continue;
                string cat = MoveCategory(l.Relation);
                if (cat == null) continue;
                foreach (string loc in l.Location.Split('|'))
                {
                    string key = cat + "@" + loc.Trim();
                    if (!e.Moves.Contains(key)) e.Moves.Add(key);
                }
            }
        }

        private static readonly HashSet<string> FieldZones = new HashSet<string> { "ONFIELD", "MZONE", "SZONE", "FZONE", "PZONE" };

        /// <summary>
        /// Can the response <paramref name="theirs"/> (of card <paramref name="their"/>) answer our effect <paramref name="ours"/>?
        /// Negate = the effect is negated; Disrupt = the response fires and hits the card or the board, but the effect still resolves
        /// (Ghost Ogre destroys the activating card). Unknown parts (no activation zone read) count as hit: the safe guess.
        /// </summary>
        public HitKind CanHit(CardEntry their, EffectEntry theirs, CardEntry our, EffectEntry ours)
        {
            ResponseFilter f = theirs.Response;
            if (f == null || !ours.Activates) return HitKind.None;
            if (f.Timing == "summons") return HitKind.None; // Nibiru answers a count of summons, not an effect
            // A face-up pick answers a play only when it can be activated at will (Impermanence); a trigger
            // (Aggregator ③ on going to the GY, Ray Lancer at the battle phase) fires on its own event, not in response.
            if (f.Timing == "target" && theirs.Kind.StartsWith("유발 효과")) return HitKind.None;

            bool monster = IsMonster(our.Data);
            if (f.EffectType == "monster" && !monster) return HitKind.None;
            if (f.EffectType == "spelltrap" && monster) return HitKind.None;
            if (f.NoCardActivation && ours.Kind == "카드의 발동") return HitKind.None;

            if (f.From.Count > 0 && ours.SourceZones.Count > 0)
            {
                bool inZone = ours.SourceZones.Any(z => f.From.Contains(z) || (f.From.Contains("ONFIELD") && FieldZones.Contains(z)));
                if (!inZone) return HitKind.None;
            }
            // A pick-a-face-up-card response (Impermanence) negates a monster on the field; a card activated from the hand is not there yet.
            if (f.Timing == "target" && ours.Kind == "카드의 발동" && monster) return HitKind.None;

            if (f.Categories.Count > 0)
            {
                bool any = f.Categories.Any(req => req.Contains("@") ? ours.Moves.Contains(req) : ours.Moves.Any(m => m == req || m.StartsWith(req + "@")));
                if (!any) return HitKind.None;
            }
            bool negates = theirs.NegateScope != null || theirs.Categories.Contains("NEGATE") || theirs.Categories.Contains("DISABLE") || f.DeclaredName;
            return negates ? HitKind.Negate : HitKind.Disrupt;
        }

        /// <summary>Cards on the other side whose responses can answer this effect, e.g. "Ash Blossom & Joyous Spring ×2(무효)".</summary>
        public List<string> Hitters(CardEntry c, EffectEntry e)
        {
            List<string> r = new List<string>();
            foreach (CardEntry o in Cards.Values)
            {
                int copies = c.Opponent ? (o.Opponent ? 0 : o.CopiesInDeck) : (o.Opponent ? Math.Max(1, o.OpponentCopies) : o.OpponentCopies);
                if (copies <= 0) continue;
                HitKind best = HitKind.None;
                foreach (EffectEntry oe in o.Effects)
                {
                    HitKind h = CanHit(o, oe, c, e);
                    if (h == HitKind.Negate || (h == HitKind.Disrupt && best == HitKind.None)) best = h;
                }
                bool named = best == HitKind.Negate && o.Effects.Any(x => x.Response != null && x.Response.DeclaredName);
                if (best != HitKind.None) r.Add(o.Data.Name + " ×" + copies + (named ? "(무효, 이름 선언)" : best == HitKind.Negate ? "(무효)" : "(방해)"));
            }
            return r;
        }

        private void BuildEffects(CardEntry c, LuaScript s)
        {
            int index = 0;
            foreach (EffectDef def in s.Effects)
            {
                // A clone that only changes the event is folded into its source effect.
                if (def.ClonedFrom != null)
                {
                    EffectEntry source = c.Effects.LastOrDefault(x => x.DescIndex == def.DescIndex && def.DescIndex != null);
                    if (source != null && def.CodeExpr != null)
                    {
                        foreach (string ev in Names(def.CodeExpr, "EVENT_")) if (!source.Events.Contains(ev)) source.Events.Add(ev);
                        continue;
                    }
                }

                EffectEntry e = new EffectEntry();
                e.Index = ++index;
                e.DescIndex = def.DescIndex;
                e.Label = def.DescIndex.HasValue && def.DescIndex.Value < 16 ? c.Data.Strings[def.DescIndex.Value] : "";
                e.Kind = ClassifyKind(def);
                if (def.CodeExpr != null && def.CodeExpr.Contains("EVENT_")) e.Events.AddRange(Names(def.CodeExpr, "EVENT_"));
                e.Categories.AddRange(Names(def.CategoryExpr, "CATEGORY_"));
                e.Range = def.RangeExpr != null ? String.Join("|", Names(def.RangeExpr, "LOCATION_").ToArray()) : null;
                e.Limit = ReadCountLimit(def, c.Data.Id);
                e.UsageKey = e.Limit != null ? e.Limit.Key : "card" + c.Data.Id + ":effect" + e.Index;
                e.DetachCount = s.DetachCount(def.Condition, def.Cost, def.Target, def.Operation);
                e.BlocksNormalSummonOfAdded = def.Operation != null && s.Mentions(def.Operation, "EFFECT_CANNOT_SUMMON");
                string cond = def.Condition == null ? "" : def.Condition.StartsWith("(inline)") ? def.Condition : (s.ExpandedBody(def.Condition) ?? "");
                e.RequiresEmptyField = cond.Contains("LOCATION_ONFIELD,0)==0");
                e.SummonsNegated = def.Operation != null && !def.Operation.StartsWith("(inline)")
                                   && (s.Mentions(def.Operation, "NegateEffects") || s.Mentions(def.Operation, "EFFECT_DISABLE"));
                if (def.CodeExpr != null && def.CodeExpr.Contains("EFFECT_TRAP_ACT_IN_HAND"))
                {
                    c.TrapActInHand = true;
                    c.TrapActInHandNeedsEmptyField = cond.Contains("LOCATION_ONFIELD,0)==0");
                }

                if (def.CodeExpr != null && def.CodeExpr.Contains("EFFECT_CANNOT_SPECIAL_SUMMON"))
                {
                    string limit = DeepText(s, def.Target);
                    if (limit.Contains("SUMMON_TYPE_XYZ") && limit.Contains("GetCode()")) c.NoSameNameXyz = true;
                }
                ReadBattleStats(c, e, def, s);
                ReadTurnLocks(e, def, s);
                e.Guard = ReadBattleGuard(def, s, cond);
                e.NegateScope = ReadNegateScope(e, def, s);
                e.Response = ReadResponse(e, def, s);
                e.SourceStop = ReadSourceStop(def, s);
                e.Reach = ReadRemovalReach(e, def, s);
                e.Shield = ReadShield(def, s, c);
                e.DrawWatch = ReadDrawWatch(c, def, s);
                if ((def.CodeExpr ?? "").Contains("EFFECT_CANNOT_ACTIVATE"))
                {
                    Match atk = Regex.Match(DeepText(s, def.ValueExpr) + " " + (def.ValueExpr ?? ""), @"IsMonsterEffect\(\)[\s\S]*?IsAttackAbove\((\d+)\)");
                    if (atk.Success)
                    {
                        e.LockAtkAbove = int.Parse(atk.Groups[1].Value, CultureInfo.InvariantCulture);
                        e.LockBoth = Regex.IsMatch(def.TargetRangeExpr ?? "", @"^\s*1\s*,\s*1\s*$");
                    }
                }
                e.GuardsSelf = e.Kind == "유발 즉시 효과" && Regex.IsMatch(DeepText(s, def.Operation), @"EFFECT_INDESTRUCTABLE_EFFECT[\s\S]*?(?<![\w.])c:RegisterEffect\(");
                AddLinks(e, s, def);
                ReadProfile(c, e);
                foreach (string f in new[] { def.Condition, def.Cost, def.Target, def.Operation })
                    if (f != null && f.StartsWith("(inline)")) e.Unresolved.Add("inline function: " + f.Substring(9));
                c.Effects.Add(e);
            }

            if (s.Material != null)
            {
                CardLink m = new CardLink();
                m.Role = "material";
                m.Relation = s.Material.Kind == "Xyz"
                    ? "엑시즈 소재 (레벨 " + s.Material.Level + " × " + s.Material.Min + (s.Material.Max > s.Material.Min ? "장 이상" : "장") + ")"
                    : "링크 소재 (" + s.Material.Min + "~" + s.Material.Max + "장)";
                m.Location = "MZONE";
                m.Filter = s.ReadFilter(s.Material.FilterExpr);
                c.Material = m;
                if (s.Material.Kind == "Xyz") ReadXyzVariants(c, s);
            }
        }

        private static readonly Regex AtkPerRank = new Regex(@"GetSum\(\s*Card\.GetRank\s*\)\s*\*\s*(\d+)", RegexOptions.Compiled);
        private static readonly Regex HandFilterUse = new Regex(@"(?:GetMatchingGroup|IsExistingMatchingCard|SelectMatchingCard)\(\s*s\.(\w+)\s*,\s*tp\s*,\s*LOCATION_HAND\b", RegexOptions.Compiled);

        private static string FnName(string expr) { return expr != null && expr.StartsWith("s.") ? expr.Substring(2) : expr; }

        /// <summary>F0 (2 Xyz monsters with the same Rank) and Ray Lancer (on top of a Rank 4 or lower Xyz, discarding a Spell/Trap).</summary>
        private static void ReadXyzVariants(CardEntry c, LuaScript s)
        {
            MaterialDef md = s.Material;
            if (c.Material.Filter.Predicates.Any(p => p.Method == "IsType" && p.Args.Any(a => a.Contains("TYPE_XYZ"))))
            {
                c.XyzOfXyz = true;
                c.SameRankMaterials = md.GroupCheck != null && DeepText(s, FnName(md.GroupCheck)).Contains("GetClassCount(Card.GetRank)==1");
                c.Material.Relation = "엑시즈 소재 (" + (c.SameRankMaterials ? "같은 랭크 " : "") + "엑시즈 몬스터 × " + md.Min + (md.Max > md.Min ? "장 이상" : "장") + ")";
            }
            if (md.AltFilterExpr == null) return;
            FilterInfo alt = s.ReadFilter(md.AltFilterExpr);
            if (!alt.Predicates.Any(p => p.Method == "IsType" && p.Args.Any(a => a.Contains("TYPE_XYZ"))))
            {
                c.Notes.Add("엑시즈가 아닌 몬스터 위 소환 미해석: " + md.AltFilterExpr);
                return;
            }
            OverlayProc o = new OverlayProc();
            foreach (Predicate p in alt.Predicates)
            {
                int n;
                if (p.Method == "IsRankBelow" && p.Args.Count > 0 && int.TryParse(p.Args[0], out n)) o.RankMax = n;
                else if (p.Method == "IsRank" && p.Args.Count > 0 && int.TryParse(p.Args[0], out n)) o.RankExact = n;
            }
            string op = md.AltOp != null ? DeepText(s, FnName(md.AltOp)) : "";
            o.OncePerTurn = Regex.IsMatch(op, @"(?:Has|Get)FlagEffect\(\s*tp\s*,\s*id\s*\)");
            Match h = HandFilterUse.Match(op);
            if (op.Contains("REASON_DISCARD") && h.Success) o.DiscardFilter = s.ReadFilter("s." + h.Groups[1].Value);
            o.Text = "엑시즈 몬스터 1장 위 (" + (o.RankExact >= 0 ? "랭크 " + o.RankExact : o.RankMax < 99 ? "랭크 " + o.RankMax + " 이하" : "랭크 무관") + ")"
                     + (o.DiscardFilter != null ? ", 패 1장 버림" : "") + (o.OncePerTurn ? ", 1턴 1번" : "");
            c.Overlay = o;
        }

        public const int AnyXyzRank = 99;
        private static readonly Regex XyzRankOnly = new Regex(@"not\s*\(\s*c:IsType\(\s*TYPE_XYZ\s*\)\s*and\s*c:IsRank\(\s*(\d+)\s*\)\s*\)", RegexOptions.Compiled);

        /// <summary>
        /// Locks an effect puts on us for the rest of the turn from its operation (registered with Duel.RegisterEffect):
        /// "cannot Special Summon from the Extra Deck, except Rank N Xyz" and "cannot declare attacks, except with Rank N Xyz".
        /// </summary>
        private static void ReadTurnLocks(EffectEntry e, EffectDef def, LuaScript s)
        {
            if (def.Operation == null || def.Operation.StartsWith("(inline)")) return;
            string op = s.Body(def.Operation) ?? "";
            if (!op.Contains("Duel.RegisterEffect")) return;
            foreach (string block in Regex.Split(op, @"Effect\.CreateEffect\("))
            {
                Match only = XyzRankOnly.Match(block);
                if (block.Contains("EFFECT_CANNOT_SPECIAL_SUMMON") && block.Contains("LOCATION_EXTRA"))
                    e.ExtraLockRank = only.Success ? int.Parse(only.Groups[1].Value, CultureInfo.InvariantCulture)
                                    : Regex.IsMatch(block, @"not\s+c:IsType\(\s*TYPE_XYZ\s*\)\s*end") ? AnyXyzRank : -2;
                else if (block.Contains("EFFECT_CANNOT_ATTACK_ANNOUNCE") && only.Success)
                    e.AttackLockRank = int.Parse(only.Groups[1].Value, CultureInfo.InvariantCulture);
            }
        }

        private static void ReadBattleStats(CardEntry c, EffectEntry e, EffectDef def, LuaScript s)
        {
            string code = def.CodeExpr ?? "";
            string value = def.ValueExpr ?? "";
            if (code.Contains("EFFECT_UPDATE_ATTACK"))
            {
                Match m = AtkPerRank.Match(value);
                if (m.Success && value.Contains("TYPE_XYZ"))
                {
                    c.Battle.AtkPerXyzRank = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                    c.Battle.RanksInOpponentGrave = Regex.IsMatch(value, @"LOCATION_MZONE\s*,\s*LOCATION_GRAVE");
                }
                int v;
                string tr = def.TargetRangeExpr ?? "";
                if ((def.TypeExpr ?? "").Contains("EFFECT_TYPE_FIELD") && Regex.IsMatch(tr, @"^\s*0\s*,\s*LOCATION_MZONE\s*$")
                    && int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out v))
                    c.Battle.OpponentAtk += v;
            }
            if (e.Events.Contains("BATTLE_DESTROYING") && (def.Operation ?? "").Contains("ChainAttack"))
                c.Battle.ChainAttackDetach = e.DetachCount;
            if (e.Events.Any(x => x.Contains("BATTLE_START")) && e.Categories.Contains("DISABLE"))
            {
                string t = DeepText(s, def.Target) + DeepText(s, def.Operation);
                c.Battle.BattleStartNegate = t.Contains("IsAttackPos") ? "attack" : "all";
            }
        }

        private static readonly Regex CardFilterCall = new Regex(@"s\.(\w+)\(\s*c\b", RegexOptions.Compiled);
        private static readonly Regex CardLambda = new Regex(@"function\s*\(\s*e\s*,\s*c\s*\)\s*return\s+(.*?)\s+end", RegexOptions.Compiled);
        private int _guardFilters;

        private BattleGuard ReadBattleGuard(EffectDef def, LuaScript s, string cond)
        {
            string code = def.CodeExpr ?? "";
            bool indestructible = code.Contains("EFFECT_INDESTRUCTABLE_BATTLE"), replace = code.Contains("EFFECT_DESTROY_REPLACE");
            if (!indestructible && !replace) return null;
            BattleGuard g = new BattleGuard { Replace = replace && !indestructible };
            g.SelfOnly = Names(def.TypeExpr, "EFFECT_TYPE_").Contains("SINGLE");
            string target = def.Target ?? "";
            if (!g.SelfOnly)
            {
                // Which monsters: a card filter called from the value / target function, or an inline "function(e,c) return ... end".
                string expr = (def.ValueExpr ?? "") + " " + target;
                Match call = CardFilterCall.Match(expr);
                Match lambda = CardLambda.Match(expr);
                if (call.Success) g.Filter = s.ReadFilter("s." + call.Groups[1].Value);
                else if (lambda.Success)
                {
                    string name = "__guard" + (++_guardFilters);
                    s.Functions[name] = "return " + lambda.Groups[1].Value + "\n";
                    g.Filter = s.ReadFilter("s." + name);
                    g.Filter.Raw = lambda.Groups[1].Value;
                }
                else if (target.Length > 0 && !target.StartsWith("(inline)")) g.Filter = s.ReadFilter("s." + target); // SetTarget(s.imtg): function(e,c)
            }
            string targetBody = target.StartsWith("(inline)") ? target : (s.ExpandedBody(target) ?? "");
            g.NeedsMaterial = cond.Contains("GetOverlayCount()>0") || targetBody.Contains("RemoveOverlayCard");
            return g;
        }

        /// <summary>Does this guard of the holder cover the target monster (static predicates only)?</summary>
        public bool GuardCovers(BattleGuard g, CardEntry holder, CardEntry target)
        {
            if (g.SelfOnly) return holder == target;
            return target == null || Matches(target, g.Filter, holder.Data.Id);
        }

        private string ClassifyKind(EffectDef d)
        {
            List<string> type = Names(d.TypeExpr, "EFFECT_TYPE_");
            string code = d.CodeExpr ?? "";
            if (type.Contains("FIELD") && code.Contains("EFFECT_SPSUMMON_PROC")) return "룰 특수 소환 (체인 없음)";
            if (type.Contains("TRIGGER_O")) return "유발 효과 (임의)";
            if (type.Contains("TRIGGER_F")) return "유발 효과 (강제)";
            if (type.Contains("QUICK_O") || type.Contains("QUICK_F")) return "유발 즉시 효과";
            if (type.Contains("IGNITION")) return "기동 효과";
            if (type.Contains("ACTIVATE")) return "카드의 발동";
            if (type.Contains("CONTINUOUS")) return "지속 처리 (" + code.Replace("EFFECT_", "").Replace("EVENT_", "") + ")";
            return "적용 효과 (" + String.Join("+", type.ToArray()) + (code.Length > 0 ? ", " + code.Replace("EFFECT_", "") : "") + ")";
        }

        private CountLimit ReadCountLimit(EffectDef def, int cardId)
        {
            if (String.IsNullOrEmpty(def.CountLimitExpr)) return null;
            List<string> a = LuaScript.SplitArgs(def.CountLimitExpr);
            CountLimit l = new CountLimit { Raw = def.CountLimitExpr };
            int n;
            if (a.Count > 0 && int.TryParse(a[0], out n)) l.Count = n;
            string flags = a.Count > 2 ? a[2] : "";
            l.Oath = flags.Contains("OATH");
            l.PerDuel = flags.Contains("DUEL");
            if (a.Count > 1)
            {
                string keyExpr = a[1].Trim();
                if (keyExpr.StartsWith("{"))
                {
                    List<string> parts = LuaScript.SplitArgs(keyExpr.Trim('{', '}'));
                    long code;
                    string codeText = parts.Count > 0 && _constants.TryEval(parts[0], cardId, out code) ? code.ToString(CultureInfo.InvariantCulture) : parts[0];
                    l.Key = codeText + "#" + (parts.Count > 1 ? parts[1] : "0");
                }
                else
                {
                    long code;
                    l.Key = (_constants.TryEval(keyExpr, cardId, out code) ? code.ToString(CultureInfo.InvariantCulture) : keyExpr) + "#0";
                }
            }
            else l.Key = "card" + cardId + ":effect" + def.Order; // soft once per turn
            return l;
        }

        private void AddLinks(EffectEntry e, LuaScript s, EffectDef def)
        {
            // (role, selection) for every function of the effect.
            List<KeyValuePair<string, Selection>> all = new List<KeyValuePair<string, Selection>>();
            foreach (var f in new[] { new KeyValuePair<string, string>("condition", def.Condition), new KeyValuePair<string, string>("cost", def.Cost),
                                      new KeyValuePair<string, string>("effect", def.Target), new KeyValuePair<string, string>("effect", def.Operation) })
            {
                if (f.Value == null || f.Value.StartsWith("(inline)")) continue;
                foreach (Selection sel in s.Selections(f.Value)) all.Add(new KeyValuePair<string, Selection>(f.Key, sel));
            }
            // Filters that are actually picked from (not only checked).
            HashSet<string> picked = new HashSet<string>(all.Where(x => !x.Value.IsCheck).Select(x => x.Value.Filter + "@" + x.Value.SelfLocation + "@" + x.Value.OpponentLocation));
            HashSet<string> seen = new HashSet<string>();

            foreach (var item in all)
            {
                Selection sel = item.Value;
                string role = item.Key;
                string where = sel.Filter + "@" + sel.SelfLocation + "@" + sel.OpponentLocation;
                if (sel.IsCheck)
                {
                    if (picked.Contains(where) && !sel.Negated) continue; // legality check of a later pick
                    role = "condition";
                }
                if (!seen.Add(role + "|" + where + "|" + sel.Negated)) continue;

                bool self = sel.SelfLocation != "0";
                string loc = Loc(self ? sel.SelfLocation : sel.OpponentLocation);
                CardLink link = new CardLink();
                link.Role = role;
                link.Location = loc;
                link.Opponent = !self;
                link.SourceFunction = sel.Function;
                link.Count = sel.Min;
                link.Filter = s.ReadFilter(sel.Filter);
                link.Filter.Raw = sel.Filter + (link.Filter.Raw != null && link.Filter.Raw != sel.Filter ? " = " + link.Filter.Raw : "");
                if (role == "condition")
                    link.Relation = sel.Negated ? "부재 조건 (" + LocKo(loc) + "에 없어야 함)" : "존재 조건 (" + LocKo(loc) + "에 있어야 함)";
                else
                {
                    // A target function usually only checks; the action is in the operation.
                    string actionFunction = role == "effect" && def.Operation != null && !def.Operation.StartsWith("(inline)") ? def.Operation : sel.Function;
                    // The picked cards are used by the next card-moving call after the pick
                    // (e.g. Plugin: pick from deck → Overlay, not the earlier SpecialSummon).
                    List<string> calls = new List<string>();
                    string action = ActionFor(s, sel, actionFunction);
                    if (action != null) calls.Add(action);
                    else
                    {
                        calls.AddRange(s.DuelCalls(sel.Function));
                        calls.AddRange(s.DuelCalls(actionFunction));
                    }
                    bool cost = role == "cost" || s.Mentions(sel.Function, "REASON_COST") || (sel.Function != actionFunction && s.Mentions(actionFunction, "REASON_COST") && !calls.Contains("SendtoHand") && !calls.Contains("SpecialSummon"));
                    link.Relation = Relation(cost, calls, loc, e);
                    if (cost) link.Role = "cost";
                }
                e.Links.Add(link);
            }
        }

        private static string Loc(string expr)
        {
            return String.Join("|", expr.Split('|').Select(x => x.Trim().Replace("LOCATION_", "")).ToArray());
        }

        private static string ActionFor(LuaScript s, Selection sel, string actionFunction)
        {
            // 0) an Extra Deck pick that is later special summoned (materials overlaid onto it first, e.g. Dark Rebellion)
            if (sel.SelfLocation.Contains("LOCATION_EXTRA"))
                foreach (var a in s.ActionCalls(sel.Function))
                    if (a.Key > sel.Index && a.Value == "SpecialSummon") return a.Value;
            // 1) a card-moving call after the pick in the same function
            foreach (var a in s.ActionCalls(sel.Function))
                if (a.Key > sel.Index) return a.Value;
            // 2) a pick in the target function is used by the first action of the operation
            if (actionFunction != sel.Function)
            {
                List<KeyValuePair<int, string>> op = s.ActionCalls(actionFunction);
                if (op.Count > 0) return op[0].Value;
            }
            return null;
        }

        private string Relation(bool cost, List<string> calls, string loc, EffectEntry e)
        {
            if (calls.Count == 1 && calls[0] == "ConfirmCards") return (cost ? "코스트: " : "") + LocKo(loc) + "의 카드를 공개";
            if (calls.Count == 1 && (calls[0] == "XyzSummon" || calls[0] == "SynchroSummon" || calls[0] == "LinkSummon"))
            {
                string kind = calls[0] == "XyzSummon" ? "엑시즈" : calls[0] == "SynchroSummon" ? "싱크로" : "링크";
                return loc == "EXTRA" ? "엑스트라 덱에서 " + kind + " 소환" : LocKo(loc) + "의 카드를 소재로 " + kind + " 소환";
            }
            List<string> actions = new List<string>(calls);
            if (!actions.Any(IsAction)) actions.AddRange(e.Categories.Select(x => "CATEGORY_" + x));
            string prefix = cost ? "코스트: " : "";
            if (actions.Contains("SpecialSummon") || actions.Contains("CATEGORY_SPECIAL_SUMMON")) return prefix + LocKo(loc) + "에서 특수 소환";
            if (actions.Contains("SendtoHand") || actions.Contains("CATEGORY_TOHAND") || actions.Contains("CATEGORY_SEARCH")) return prefix + LocKo(loc) + "에서 패에 넣기";
            if (actions.Contains("Overlay")) return prefix + LocKo(loc) + "에서 엑시즈 소재로";
            if (actions.Contains("SSet")) return prefix + LocKo(loc) + "에서 세트";
            if (actions.Contains("Destroy") || actions.Contains("CATEGORY_DESTROY")) return prefix + LocKo(loc) + "의 카드 파괴";
            if (actions.Contains("SendtoGrave") || actions.Contains("CATEGORY_TOGRAVE")) return prefix + LocKo(loc) + "에서 묘지로";
            if (actions.Contains("SendtoDeck") || actions.Contains("CATEGORY_TODECK")) return prefix + LocKo(loc) + "에서 덱으로";
            if (actions.Contains("Remove") || actions.Contains("CATEGORY_REMOVE")) return prefix + LocKo(loc) + "에서 제외";
            if (actions.Contains("Release") || actions.Contains("CATEGORY_RELEASE")) return prefix + LocKo(loc) + "에서 릴리스";
            return prefix + LocKo(loc) + "의 카드 선택 (처리 미해석)";
        }

        private static bool IsAction(string call)
        {
            switch (call)
            {
                case "SpecialSummon": case "SendtoHand": case "Overlay": case "SSet": case "Destroy":
                case "SendtoGrave": case "SendtoDeck": case "Remove": case "Release":
                    return true;
            }
            return false;
        }

        private static string LocKo(string loc)
        {
            List<string> ko = new List<string>();
            foreach (string p in loc.Split('|'))
            {
                switch (p)
                {
                    case "DECK": ko.Add("덱"); break;
                    case "HAND": ko.Add("패"); break;
                    case "GRAVE": ko.Add("묘지"); break;
                    case "REMOVED": ko.Add("제외"); break;
                    case "EXTRA": ko.Add("엑스트라 덱"); break;
                    case "MZONE": ko.Add("몬스터 존"); break;
                    case "SZONE": ko.Add("마법·함정 존"); break;
                    case "ONFIELD": ko.Add("필드"); break;
                    case "OVERLAY": ko.Add("엑시즈 소재"); break;
                    default: ko.Add(p); break;
                }
            }
            return String.Join("/", ko.ToArray());
        }

        // ------------------------------------------------------------------ links

        private void ComputeLinks()
        {
            foreach (CardEntry c in Cards.Values) c.LinkedFrom.Clear();
            foreach (CardEntry c in Cards.Values) ComputeLinksFor(c);
        }

        private void ComputeLinksFor(CardEntry c)
        {
            foreach (EffectEntry e in c.Effects)
                foreach (CardLink l in e.Links)
                {
                    l.Targets.Clear();
                    // Whose cards the link looks at: the owner's own deck, or the other side's for "opponent" links.
                    bool theirs = l.Opponent ? !c.Opponent : c.Opponent;
                    if (theirs && !HasOpponentDeck) continue;
                    if (l.Filter.Partial)
                    {
                        // Not evaluated: or / not(...) / local logic. Keep the raw text instead of a wrong list.
                        if (!e.Unresolved.Contains("filter " + l.Filter.Raw)) e.Unresolved.Add("filter " + l.Filter.Raw);
                        continue;
                    }
                    l.AnyCard = !l.Filter.Predicates.Any(p => StaticMethods.Contains(p.Method))
                                && !l.Filter.Groups.Any(g => g.Items.All(p => StaticMethods.Contains(p.Method)));
                    if (l.AnyCard) continue;
                    bool monstersOnly = l.Relation.Contains("특수 소환") || l.Location == "MZONE";
                    foreach (int id in Candidates(l.Location, theirs))
                        if ((!monstersOnly || IsMonster(Cards[id].Data)) && Matches(Cards[id], l.Filter, c.Data.Id))
                        {
                            l.Targets.Add(id);
                            Cards[id].LinkedFrom.Add(new KeyValuePair<int, string>(c.Data.Id, "효과 " + e.Index + ": " + l.Relation));
                        }
                }

            if (c.Overlay != null)
            {
                c.Overlay.DiscardTargets.Clear();
                if (c.Overlay.DiscardFilter != null && !c.Overlay.DiscardFilter.Partial)
                    foreach (int id in (c.Opponent ? OpponentMainDeck : MainDeck).Distinct())
                        if (Matches(Cards[id], c.Overlay.DiscardFilter, c.Data.Id)) c.Overlay.DiscardTargets.Add(id);
            }
            if (c.Material != null && c.XyzOfXyz)
            {
                c.Material.Targets.Clear();
                foreach (int id in (c.Opponent ? OpponentExtraDeck : ExtraDeck).Distinct())
                    if (id != c.Data.Id && IsType(Cards[id].Data, "TYPE_XYZ"))
                    {
                        c.Material.Targets.Add(id);
                        Cards[id].LinkedFrom.Add(new KeyValuePair<int, string>(c.Data.Id, "소재로 쓰여 소환됨: " + c.Material.Relation));
                    }
            }
            else if (c.Material != null)
            {
                c.Material.Targets.Clear();
                foreach (int id in (c.Opponent ? OpponentMainDeck : MainDeck).Distinct())
                {
                    CardData d = Cards[id].Data;
                    if (!IsMonster(d) || IsType(d, "TYPE_XYZ") || IsType(d, "TYPE_LINK")) continue;
                    if (c.Material.Relation.StartsWith("엑시즈") && d.Level != MaterialLevel(c)) continue;
                    if (!Matches(Cards[id], c.Material.Filter, c.Data.Id)) continue;
                    c.Material.Targets.Add(id);
                    Cards[id].LinkedFrom.Add(new KeyValuePair<int, string>(c.Data.Id, "소재로 쓰여 소환됨: " + c.Material.Relation));
                }
            }
        }

        private int MaterialLevel(CardEntry c)
        {
            // "엑시즈 소재 (레벨 N × ..."
            string r = c.Material.Relation;
            int start = r.IndexOf("레벨 ", StringComparison.Ordinal) + 3;
            int end = r.IndexOf(' ', start);
            int lv;
            return int.TryParse(r.Substring(start, end - start), out lv) ? lv : -1;
        }

        private IEnumerable<int> Candidates(string loc, bool opponentSide)
        {
            List<int> main = opponentSide ? OpponentMainDeck : MainDeck;
            List<int> extra = opponentSide ? OpponentExtraDeck : ExtraDeck;
            HashSet<int> ids = new HashSet<int>();
            foreach (string p in loc.Split('|'))
            {
                if (p == "EXTRA") foreach (int id in extra) ids.Add(id);
                else if (p == "DECK" || p == "HAND") foreach (int id in main) ids.Add(id);
                else { foreach (int id in main) ids.Add(id); foreach (int id in extra) ids.Add(id); }
            }
            return ids;
        }

        private static readonly HashSet<string> StaticMethods = new HashSet<string>
        {
            "IsMonster", "IsSpell", "IsTrap", "IsSpellTrap", "IsType", "IsOriginalType", "IsAttribute", "IsOriginalAttribute",
            "IsRace", "IsOriginalRace", "IsSetCard", "IsOriginalSetCard", "IsCode", "IsOriginalCode", "IsLevel", "IsOriginalLevel",
            "IsRank", "IsOriginalRank", "IsLink", "IsLevelBelow", "IsLevelAbove", "IsAttackBelow", "IsAttackAbove",
            "IsDefenseBelow", "IsDefenseAbove"
        };

        private bool IsType(CardData d, string name) { return (d.Type & Const(name)) != 0; }
        /// <summary>Fusion / Synchro / Xyz / Link: lives in the Extra Deck, never in the hand.</summary>
        public bool IsMonsterCard(CardData d) { return IsMonster(d); }
        public bool IsQuickPlay(CardData d) { return IsType(d, "TYPE_SPELL") && IsType(d, "TYPE_QUICKPLAY"); }
        public bool IsExtraDeckCard(CardData d) { return IsType(d, "TYPE_FUSION") || IsType(d, "TYPE_SYNCHRO") || IsType(d, "TYPE_XYZ") || IsType(d, "TYPE_LINK"); }
        private bool IsMonster(CardData d) { return IsType(d, "TYPE_MONSTER"); }

        /// <summary>Static predicates are checked against cdb data; runtime ones (IsFaceup, IsAbleTo...) are ignored here.</summary>
        private bool Matches(CardEntry card, FilterInfo f, int selfId)
        {
            if (f == null) return true;
            CardData d = card.Data;
            foreach (Predicate p in f.Predicates)
            {
                bool? r = Evaluate(d, p, selfId);
                if (r == null) continue;
                if (r.Value == p.Negated) return false;
            }
            foreach (PredicateGroup g in f.Groups)
            {
                // OR over the items; runtime-only items are unknown and make the group count as satisfied.
                bool any = false, unknown = false;
                foreach (Predicate p in g.Items)
                {
                    bool? r = Evaluate(d, p, selfId);
                    if (r == null) { unknown = true; continue; }
                    if (r.Value != p.Negated) { any = true; break; }
                }
                if (unknown && !any) continue;
                if (any == g.Negated) return false;
            }
            return true;
        }

        private bool? Evaluate(CardData d, Predicate p, int selfId)
        {
            long v;
            string a0 = p.Args.Count > 0 ? p.Args[0] : null;
            switch (p.Method)
            {
                case "IsMonster": return IsMonster(d);
                case "IsSpell": return IsType(d, "TYPE_SPELL");
                case "IsTrap": return IsType(d, "TYPE_TRAP");
                case "IsSpellTrap": return IsType(d, "TYPE_SPELL") || IsType(d, "TYPE_TRAP");
                case "IsType":
                case "IsOriginalType":
                    return a0 != null && _constants.TryEval(a0, selfId, out v) ? (d.Type & v) != 0 : (bool?)null;
                case "IsAttribute":
                case "IsOriginalAttribute":
                    return a0 != null && _constants.TryEval(a0, selfId, out v) ? (d.Attribute & v) != 0 : (bool?)null;
                case "IsRace":
                case "IsOriginalRace":
                    return a0 != null && _constants.TryEval(a0, selfId, out v) ? (d.Race & v) != 0 : (bool?)null;
                case "IsSetCard":
                case "IsOriginalSetCard":
                    return a0 != null && _constants.TryEval(a0, selfId, out v) ? HasSetcode(d.Setcode, v) : (bool?)null;
                case "IsCode":
                case "IsOriginalCode":
                    foreach (string arg in p.Args)
                        if (_constants.TryEval(arg, selfId, out v) && (v == d.Id || (d.Alias != 0 && v == d.Alias))) return true;
                    return false;
                case "IsLevel":
                case "IsOriginalLevel":
                    if (!IsMonster(d) || IsType(d, "TYPE_XYZ") || IsType(d, "TYPE_LINK")) return false;
                    return AnyArgEquals(p, d.Level, selfId);
                case "IsRank":
                case "IsOriginalRank":
                    if (!IsType(d, "TYPE_XYZ")) return false;
                    return AnyArgEquals(p, d.Level, selfId);
                case "IsLink":
                    if (!IsType(d, "TYPE_LINK")) return false;
                    return AnyArgEquals(p, d.Level, selfId);
                case "IsLevelBelow": return Compare(p, d.Level, selfId, true) && !IsType(d, "TYPE_XYZ") && !IsType(d, "TYPE_LINK");
                case "IsLevelAbove": return Compare(p, d.Level, selfId, false) && !IsType(d, "TYPE_XYZ") && !IsType(d, "TYPE_LINK");
                case "IsAttackBelow": return Compare(p, d.Atk, selfId, true);
                case "IsAttackAbove": return Compare(p, d.Atk, selfId, false);
                case "IsDefenseBelow": return Compare(p, d.Def, selfId, true);
                case "IsDefenseAbove": return Compare(p, d.Def, selfId, false);
                default: return null; // runtime-dependent (IsFaceup, IsAbleToHand, IsCanBeSpecialSummoned, ...)
            }
        }

        private bool AnyArgEquals(Predicate p, int value, int selfId)
        {
            long v;
            foreach (string arg in p.Args)
                if (_constants.TryEval(arg, selfId, out v) && v == value) return true;
            return false;
        }

        private bool Compare(Predicate p, int value, int selfId, bool below)
        {
            long v;
            if (p.Args.Count == 0 || !_constants.TryEval(p.Args[0], selfId, out v)) return true;
            return below ? value <= v : value >= v;
        }

        private static bool HasSetcode(long cardSetcodes, long set)
        {
            for (int i = 0; i < 4; ++i)
            {
                long s = (cardSetcodes >> (16 * i)) & 0xffff;
                if (s == 0) continue;
                if ((s & 0xfff) == (set & 0xfff) && (s & set) == set) return true;
            }
            return false;
        }

        // ------------------------------------------------------------------ runtime state

        public void OnNewTurn(int turn)
        {
            Turn = turn;
            _usage[0].Clear();
            _usage[1].Clear();
            NormalSummonUsed = false;
            OpponentNormalSummonUsed = false;
            TurnEvents.Clear();
            _drawWatch[0].Clear();
            _drawWatch[1].Clear();
        }

        private static string KeyOf(CardEntry card, EffectEntry e)
        {
            if (e != null) return e.UsageKey;
            return "card" + (card != null ? card.Data.Id : 0) + ":unknown";
        }

        private Dictionary<string, KeyUsage> TableOf(int player, EffectEntry e)
        {
            return e != null && e.Limit != null && e.Limit.PerDuel ? _duelUsage[player] : _usage[player];
        }

        private KeyUsage Usage(int player, CardEntry card, EffectEntry e)
        {
            Dictionary<string, KeyUsage> table = TableOf(player, e);
            string key = KeyOf(card, e);
            KeyUsage u;
            if (!table.TryGetValue(key, out u)) { u = new KeyUsage(); table[key] = u; }
            return u;
        }

        /// <summary>Usage without creating an entry; null when the effect was not touched.</summary>
        public KeyUsage PeekUsage(int player, EffectEntry e)
        {
            KeyUsage u;
            return TableOf(player, e).TryGetValue(e.UsageKey, out u) ? u : null;
        }

        /// <summary>Finds the effect an engine description refers to: (card code &lt;&lt; 20) | string index.</summary>
        public EffectEntry FindEffect(CardEntry card, long description)
        {
            if (card == null) return null;
            if (description > 0)
            {
                long index = description & 0xfffff;
                EffectEntry byDesc = card.Effects.FirstOrDefault(e => e.DescIndex == index);
                if (byDesc != null) return byDesc;
            }
            List<EffectEntry> activatable = card.Effects.Where(e => !e.Kind.StartsWith("룰") && !e.Kind.StartsWith("지속") && !e.Kind.StartsWith("적용")).ToList();
            return activatable.Count == 1 ? activatable[0] : null;
        }

        public void OnChaining(int chainLink, int player, int cardId, long description)
        {
            CardEntry card = EnsureCard(cardId, player == 1);
            EffectEntry e = FindEffect(card, description);
            PendingLink link = new PendingLink { Player = player, Card = card, Effect = e };
            _chain[chainLink] = link;
            KeyUsage u = Usage(player, card, e);
            u.Activated++;
            // Counted at activation for both kinds; an "activate" (OATH) limit is refunded if the activation is negated.
            u.Used++;
            TurnEvents.Add("체인 " + chainLink + ": " + Who(player) + " " + Name(card) + " " + EffectName(e) + " 발동");
        }

        /// <summary>
        /// Activation negated. Verified in EDOPro (2026-11-07): Mist Valley Apex Avian negating Bonfire
        /// ("can only activate 1 per turn", OATH) → Bonfire can be activated again. So an OATH limit is
        /// refunded; a "can only use once per turn" limit (no OATH) stays spent.
        /// </summary>
        public void OnChainNegated(int chainLink)
        {
            PendingLink l;
            if (!_chain.TryGetValue(chainLink, out l)) return;
            l.ActivationNegated = true;
            KeyUsage u = Usage(l.Player, l.Card, l.Effect);
            u.ActivationNegated++;
            bool refunded = l.Effect != null && l.Effect.Limit != null && l.Effect.Limit.Oath && u.Used > 0;
            if (refunded) u.Used--;
            TurnEvents.Add("체인 " + chainLink + ": 발동 무효 (" + Name(l.Card) + (refunded ? ", '발동' 제약이라 다시 발동 가능" : "") + ")");
        }

        public void OnChainDisabled(int chainLink)
        {
            PendingLink l;
            if (!_chain.TryGetValue(chainLink, out l)) return;
            l.EffectNegated = true;
            Usage(l.Player, l.Card, l.Effect).EffectNegated++;
            TurnEvents.Add("체인 " + chainLink + ": 효과 무효 (" + Name(l.Card) + ")");
        }

        private readonly List<DrawWatch>[] _drawWatch = { new List<DrawWatch>(), new List<DrawWatch>() };

        /// <summary>"Draw each time the other player summons" effects this player resolved this turn (Maxx "C", Mulcharmy).</summary>
        public IList<DrawWatch> DrawWatches(int player) { return _drawWatch[player]; }

        public void OnChainSolved(int chainLink)
        {
            PendingLink l;
            if (!_chain.TryGetValue(chainLink, out l)) return;
            KeyUsage u = Usage(l.Player, l.Card, l.Effect);
            if (!l.ActivationNegated && !l.EffectNegated) u.Resolved++;
            if (!l.ActivationNegated && !l.EffectNegated && l.Effect != null && l.Effect.DrawWatch != null)
            {
                _drawWatch[l.Player].Add(l.Effect.DrawWatch);
                TurnEvents.Add("드로우 감시 시작: " + Who(l.Player) + " " + l.Effect.DrawWatch.Describe());
            }
            TurnEvents.Add("체인 " + chainLink + ": 처리 (" + Name(l.Card) + (l.ActivationNegated ? ", 발동 무효됨" : l.EffectNegated ? ", 효과 무효됨" : "") + ")");
        }

        public void OnChainEnd()
        {
            _chain.Clear();
        }

        /// <summary>A rule summon procedure (no chain) of our own card was performed.</summary>
        public void OnRuleSummon(int cardId)
        {
            CardEntry card = Get(cardId);
            if (card == null) return;
            EffectEntry proc = card.Effects.FirstOrDefault(e => e.Kind.StartsWith("룰"));
            if (proc == null) return;
            KeyUsage u = Usage(0, card, proc);
            u.Activated++;
            u.Used++;
            u.Resolved++;
            TurnEvents.Add("룰 특수 소환: " + Name(card));
        }

        public void OnNormalSummon(int cardId)
        {
            NormalSummonUsed = true;
            TurnEvents.Add("일반 소환: " + Name(Get(cardId)));
        }

        /// <summary>The opponent Normal Summoned (or Set) a monster this turn: their summon right is spent.</summary>
        public void OnOpponentNormalSummon(int cardId)
        {
            OpponentNormalSummonUsed = true;
            TurnEvents.Add("상대 일반 소환: " + (cardId != 0 ? Name(EnsureCard(cardId, true)) : "(뒷면)"));
        }

        private static string Who(int player) { return player == 0 ? "자신" : "상대"; }
        private static string Name(CardEntry c) { return c == null ? "(알 수 없는 카드)" : c.Data.Name; }
        private static string EffectName(EffectEntry e) { return e == null ? "효과(미확인)" : "효과 " + e.Index; }

        public string UsageText(int player, EffectEntry e)
        {
            if (e.Limit == null && e.Kind.StartsWith("적용")) return "";
            KeyUsage u = PeekUsage(player, e) ?? new KeyUsage();
            string limit = e.Limit != null ? "/" + e.Limit.Count : "";
            return "발동 " + u.Activated + "회, 사용 " + u.Used + limit + (u.ActivationNegated > 0 ? ", 발동 무효 " + u.ActivationNegated : "")
                 + (u.EffectNegated > 0 ? ", 효과 무효 " + u.EffectNegated : "")
                 + (e.Limit != null && u.Used >= e.Limit.Count ? " → 이번 턴 더 못 씀" : "");
        }

        // ------------------------------------------------------------------ readable export

        /// <summary>
        /// Compact per-turn state: events, effects used this turn (both players), and where our cards are.
        /// Appended once per turn so the fixed card information is not repeated.
        /// </summary>
        public string DescribeState(string title, Func<int, string> instances)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("## " + title + " (턴 " + Turn + ")");
            sb.AppendLine("- 일반 소환: " + (NormalSummonUsed ? "사용함" : "안 함"));
            if (TurnEvents.Count > 0)
            {
                sb.AppendLine("- 기록:");
                foreach (string ev in TurnEvents) sb.AppendLine("  - " + ev);
            }
            List<string> used = new List<string>();
            foreach (CardEntry c in Cards.Values)
                foreach (EffectEntry e in c.Effects)
                    for (int p = 0; p < 2; ++p)
                    {
                        KeyUsage u = PeekUsage(p, e);
                        if (u == null || u.Activated == 0) continue;
                        used.Add((p == 0 ? "자신 " : "상대 ") + c.Data.Name + " 효과 " + e.Index + ": " + UsageText(p, e));
                    }
            if (used.Count > 0)
            {
                sb.AppendLine("- 사용한 효과:");
                foreach (string s in used.Distinct()) sb.AppendLine("  - " + s);
            }
            if (instances != null)
            {
                List<string> where = new List<string>();
                foreach (CardEntry c in Cards.Values)
                {
                    string inst = instances(c.Data.Id);
                    if (!String.IsNullOrEmpty(inst)) where.Add(c.Data.Name + ": " + inst);
                }
                if (where.Count > 0)
                {
                    sb.AppendLine("- 카드 위치:");
                    foreach (string w in where) sb.AppendLine("  - " + w);
                }
            }
            sb.AppendLine();
            return sb.ToString();
        }

        /// <summary>Fixed card information and links (written once per game).</summary>
        public string Describe(Func<int, string> instances)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# 게임 카드 DB");
            sb.AppendLine();
            sb.AppendLine("- 메인 덱 " + MainDeck.Count + "장, 엑스트라 덱 " + ExtraDeck.Count + "장, DB 카드 종류 " + Cards.Count);
            sb.AppendLine("- 턴마다 바뀌는 발동·사용 기록과 카드 위치는 같은 이름의 `_state.md`에 있다.");
            sb.AppendLine();

            foreach (CardEntry c in Cards.Values.OrderBy(x => x.Opponent).ThenBy(x => x.InExtraDeck).ThenBy(x => x.Data.Name, StringComparer.Ordinal))
            {
                CardData d = c.Data;
                sb.AppendLine("## " + d.Name + " (" + d.Id + ")" + (c.Opponent ? " — 상대 카드" + (c.OpponentCopies > 0 ? " ×" + c.OpponentCopies : "") : c.CopiesInDeck > 0 ? " ×" + c.CopiesInDeck + (c.InExtraDeck ? " [엑스트라]" : "") + (c.OpponentCopies > 0 ? " (상대 덱 ×" + c.OpponentCopies + ")" : "") : ""));
                sb.AppendLine(Stats(d));
                if (c.NoSameNameXyz) sb.AppendLine("- 앞면인 동안: 같은 이름 엑시즈 몬스터 특수 소환 불가");
                if (!c.ScriptFound) sb.AppendLine("- 스크립트 없음" + (c.Notes.Count > 0 ? " (" + String.Join(", ", c.Notes.ToArray()) + ")" : ""));
                if (c.Material != null)
                    sb.AppendLine("- 소환 조건: " + c.Material.Relation + FilterText(c.Material.Filter) + " → 이 덱에서: " + NamesOf(c.Material.Targets));
                if (c.Overlay != null)
                    sb.AppendLine("- 다른 소환 방법: " + c.Overlay.Text + (c.Overlay.DiscardFilter != null ? " → 버릴 수 있는 카드: " + NamesOf(c.Overlay.DiscardTargets) : ""));
                if (c.Battle.Any)
                {
                    List<string> b = new List<string>();
                    if (c.Battle.AtkPerXyzRank != 0) b.Add("공격력 +" + c.Battle.AtkPerXyzRank + " × (자신 필드" + (c.Battle.RanksInOpponentGrave ? " + 상대 묘지" : "") + " 엑시즈 랭크 합)");
                    if (c.Battle.OpponentAtk != 0) b.Add("상대 몬스터 공격력 " + c.Battle.OpponentAtk.ToString("+0;-0", CultureInfo.InvariantCulture));
                    if (c.Battle.ChainAttackDetach >= 0) b.Add("전투 파괴 후 연속 공격 (소재 " + c.Battle.ChainAttackDetach + "개)");
                    if (c.Battle.BattleStartNegate != null) b.Add("배틀 페이즈 개시 시 상대 " + (c.Battle.BattleStartNegate == "attack" ? "공격 표시 " : "") + "몬스터 효과 무효");
                    sb.AppendLine("- 전투: " + String.Join(", ", b.ToArray()));
                }

                foreach (EffectEntry e in c.Effects)
                {
                    sb.AppendLine("- 효과 " + e.Index + (e.DescIndex.HasValue ? " [설명 " + e.DescIndex + "]" : "") + ": " + e.Kind
                        + (e.Events.Count > 0 ? " / 계기: " + String.Join(", ", e.Events.ToArray()) : "")
                        + (e.Range != null ? " / 위치: " + e.Range : ""));
                    if (!String.IsNullOrEmpty(e.Label)) sb.AppendLine("  - 설명: " + e.Label);
                    if (e.Categories.Count > 0) sb.AppendLine("  - 분류: " + String.Join(", ", e.Categories.ToArray()));
                    if (e.Limit != null) sb.AppendLine("  - 횟수: 1턴 " + e.Limit.Count + "번" + (e.Limit.Oath ? " ('발동' 제약: 발동 무효되면 다시 발동 가능)" : " ('사용' 제약: 발동 무효돼도 소비)") + (e.Limit.PerDuel ? ", 듀얼 중" : "") + ", 키 " + e.Limit.Key);
                    if (e.ExtraLockRank != -1) sb.AppendLine("  - 사용 후 이번 턴: 엑스트라 덱에서 " + (e.ExtraLockRank == AnyXyzRank ? "엑시즈만 특수 소환" : e.ExtraLockRank >= 0 ? "랭크 " + e.ExtraLockRank + " 엑시즈만 특수 소환" : "특수 소환 제약 (미해석)"));
                    if (e.AttackLockRank != -1) sb.AppendLine("  - 사용 후 이번 턴: 랭크 " + e.AttackLockRank + " 엑시즈만 공격 선언");
                    if (e.DetachCount > 0) sb.AppendLine("  - 엑시즈 소재 제거: " + e.DetachCount + "개 (필드에 그만큼 소재가 있어야 함)");
                    if (e.Response != null) sb.AppendLine("  - 응답 대상: " + ResponseText(e.Response));
                    if (e.Activates)
                    {
                        List<string> hit = Hitters(c, e);
                        sb.AppendLine("  - 발동 위치: " + (e.SourceZones.Count > 0 ? String.Join("/", e.SourceZones.ToArray()) : "모름") + (e.Moves.Count > 0 ? " / 처리: " + String.Join(", ", e.Moves.ToArray()) : ""));
                        sb.AppendLine("  - " + (c.Opponent ? "막을 수 있는 우리 카드: " : "막을 수 있는 상대 카드: ") + (hit.Count > 0 ? String.Join(", ", hit.ToArray()) : "없음"));
                    }
                    if (e.NegateScope != null) sb.AppendLine("  - 무효 대상: " + (e.NegateScope == "monster" ? "몬스터 효과" : e.NegateScope == "spelltrap" ? "마법·함정 효과" : "전부"));
                    if (e.BlocksNormalSummonOfAdded) sb.AppendLine("  - 제약: 이 효과로 패에 넣은 카드는 일반 소환할 수 없음");
                    if (e.Guard != null) sb.AppendLine("  - 전투: " + e.Guard.Describe());
                    foreach (CardLink l in e.Links)
                        sb.AppendLine("  - " + RoleKo(l.Role) + ": " + l.Relation + FilterText(l.Filter)
                            + (l.Opponent && l.Targets.Count == 0 ? " → 상대 카드"
                               : l.Filter.Partial ? " → (미해석, 연결 계산 안 함)"
                               : l.AnyCard ? " → 조건에 맞는 아무 카드"
                               : (l.Opponent != c.Opponent ? " → 상대 덱에서: " : " → 이 덱에서: ") + NamesOf(l.Targets)));
                    foreach (string u in e.Unresolved) sb.AppendLine("  - 미해석: " + u);
                }
                if (c.LinkedFrom.Count > 0)
                {
                    sb.AppendLine("- 이 카드를 불러오는 카드:");
                    foreach (var g in c.LinkedFrom.GroupBy(x => x.Key))
                        sb.AppendLine("  - " + Get(g.Key).Data.Name + " — " + String.Join("; ", g.Select(x => x.Value).Distinct().ToArray()));
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private string Stats(CardData d)
        {
            if (!d.InDatabase) return "- cdb에 없음";
            List<string> parts = new List<string>();
            parts.Add(TypeText(d));
            if (IsMonster(d))
            {
                parts.Add(FlagName(d.Attribute, "ATTRIBUTE_"));
                parts.Add(FlagName(d.Race, "RACE_"));
                parts.Add((IsType(d, "TYPE_XYZ") ? "랭크 " : IsType(d, "TYPE_LINK") ? "링크 " : "레벨 ") + d.Level);
                parts.Add(d.Atk + " / " + (IsType(d, "TYPE_LINK") ? "-" : d.Def.ToString(CultureInfo.InvariantCulture)));
            }
            string sets = SetNames(d.Setcode);
            if (sets.Length > 0) parts.Add("카드군 " + sets);
            return "- " + String.Join(" · ", parts.ToArray());
        }

        private string TypeText(CardData d)
        {
            List<string> t = new List<string>();
            foreach (string n in new[] { "MONSTER", "SPELL", "TRAP", "NORMAL", "EFFECT", "FUSION", "RITUAL", "SYNCHRO", "XYZ", "PENDULUM", "LINK", "TUNER", "SPIRIT", "QUICKPLAY", "CONTINUOUS", "EQUIP", "FIELD", "COUNTER", "TOKEN" })
                if (IsType(d, "TYPE_" + n)) t.Add(n);
            return String.Join("+", t.ToArray());
        }

        private string FlagName(long value, string prefix)
        {
            foreach (string n in KnownFlags(prefix))
            {
                long v;
                if (_constants.TryGet(prefix + n, out v) && v == value) return n;
            }
            return "0x" + value.ToString("x");
        }

        private static IEnumerable<string> KnownFlags(string prefix)
        {
            if (prefix == "ATTRIBUTE_") return new[] { "EARTH", "WATER", "FIRE", "WIND", "LIGHT", "DARK", "DIVINE" };
            return new[] { "WARRIOR", "SPELLCASTER", "FAIRY", "FIEND", "ZOMBIE", "MACHINE", "AQUA", "PYRO", "ROCK", "WINGEDBEAST", "PLANT", "INSECT", "THUNDER", "DRAGON", "BEAST", "BEASTWARRIOR", "DINOSAUR", "FISH", "SEASERPENT", "REPTILE", "PSYCHIC", "DIVINE", "CREATORGOD", "WYRM", "CYBERSE", "ILLUSION" };
        }

        private string SetNames(long setcodes)
        {
            List<string> names = new List<string>();
            for (int i = 0; i < 4; ++i)
            {
                long s = (setcodes >> (16 * i)) & 0xffff;
                if (s != 0) names.Add("0x" + s.ToString("x"));
            }
            return String.Join(",", names.ToArray());
        }

        private string NamesOf(List<int> ids)
        {
            if (ids.Count == 0) return "(없음)";
            return String.Join(", ", ids.Select(id => Get(id).Data.Name).ToArray());
        }

        private static string FilterText(FilterInfo f)
        {
            if (f == null) return "";
            if (f.Partial) return " [원문: " + f.Raw + "]";
            List<string> parts = f.Predicates.Select(p => p.ToString()).ToList();
            parts.AddRange(f.Groups.Select(g => g.ToString()));
            return parts.Count > 0 ? " [조건: " + String.Join(" and ", parts.ToArray()) + "]" : "";
        }

        private static string RoleKo(string role)
        {
            switch (role)
            {
                case "condition": return "조건";
                case "cost": return "코스트";
                case "effect": return "효과";
                default: return role;
            }
        }
    }
}
