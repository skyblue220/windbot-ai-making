using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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
    }

    public sealed class CountLimit
    {
        public int Count = 1;
        public string Key;
        public bool Oath;
        public bool PerDuel;
        public string Raw;
    }

    public sealed class EffectEntry
    {
        public int Index;
        public int? DescIndex;
        public string Label;
        public string Kind;
        public readonly List<string> Events = new List<string>();
        public readonly List<string> Categories = new List<string>();
        public string Range;
        public CountLimit Limit;
        public readonly List<CardLink> Links = new List<CardLink>();
        public readonly List<string> Unresolved = new List<string>();
        public string Confidence = "inferred";
    }

    public sealed class CardEntry
    {
        public CardData Data;
        public int CopiesInDeck;
        public bool InMainDeck;
        public bool InExtraDeck;
        public bool Opponent;
        public bool ScriptFound;
        public string ScriptPath;
        public readonly List<EffectEntry> Effects = new List<EffectEntry>();
        public CardLink Material;
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

        // runtime: per player (0 self, 1 opponent), per count-limit key
        private readonly Dictionary<string, KeyUsage>[] _usage = { new Dictionary<string, KeyUsage>(), new Dictionary<string, KeyUsage>() };
        private readonly Dictionary<string, KeyUsage>[] _duelUsage = { new Dictionary<string, KeyUsage>(), new Dictionary<string, KeyUsage>() };
        private readonly Dictionary<int, PendingLink> _chain = new Dictionary<int, PendingLink>();
        public int Turn;
        public bool NormalSummonUsed;
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

                AddLinks(e, s, def);
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
            }
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
                link.Filter = s.ReadFilter(sel.Filter);
                link.Filter.Raw = sel.Filter + (link.Filter.Raw != null && link.Filter.Raw != sel.Filter ? " = " + link.Filter.Raw : "");
                if (role == "condition")
                    link.Relation = sel.Negated ? "부재 조건 (" + LocKo(loc) + "에 없어야 함)" : "존재 조건 (" + LocKo(loc) + "에 있어야 함)";
                else
                {
                    // A target function usually only checks; the action is in the operation.
                    string actionFunction = role == "effect" && def.Operation != null && !def.Operation.StartsWith("(inline)") ? def.Operation : sel.Function;
                    List<string> calls = s.DuelCalls(sel.Function);
                    calls.AddRange(s.DuelCalls(actionFunction));
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

        private string Relation(bool cost, List<string> calls, string loc, EffectEntry e)
        {
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
                    if (l.Opponent) continue;
                    if (l.Filter.Partial)
                    {
                        // Not evaluated: or / not(...) / local logic. Keep the raw text instead of a wrong list.
                        if (!e.Unresolved.Contains("filter " + l.Filter.Raw)) e.Unresolved.Add("filter " + l.Filter.Raw);
                        continue;
                    }
                    l.AnyCard = !l.Filter.Predicates.Any(p => StaticMethods.Contains(p.Method));
                    if (l.AnyCard) continue;
                    bool monstersOnly = l.Relation.Contains("특수 소환");
                    foreach (int id in Candidates(l.Location))
                        if ((!monstersOnly || IsMonster(Cards[id].Data)) && Matches(Cards[id], l.Filter, c.Data.Id))
                        {
                            l.Targets.Add(id);
                            Cards[id].LinkedFrom.Add(new KeyValuePair<int, string>(c.Data.Id, "효과 " + e.Index + ": " + l.Relation));
                        }
                }

            if (c.Material != null)
            {
                c.Material.Targets.Clear();
                foreach (int id in MainDeck.Distinct())
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

        private IEnumerable<int> Candidates(string loc)
        {
            HashSet<int> ids = new HashSet<int>();
            foreach (string p in loc.Split('|'))
            {
                if (p == "EXTRA") foreach (int id in ExtraDeck) ids.Add(id);
                else if (p == "DECK" || p == "HAND") foreach (int id in MainDeck) ids.Add(id);
                else { foreach (int id in MainDeck) ids.Add(id); foreach (int id in ExtraDeck) ids.Add(id); }
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
            TurnEvents.Clear();
        }

        private KeyUsage Usage(int player, CountLimit limit)
        {
            Dictionary<string, KeyUsage> table = limit != null && limit.PerDuel ? _duelUsage[player] : _usage[player];
            string key = limit != null ? limit.Key : "(no limit)";
            KeyUsage u;
            if (!table.TryGetValue(key, out u)) { u = new KeyUsage(); table[key] = u; }
            return u;
        }

        public KeyUsage UsageOf(int player, EffectEntry e)
        {
            return Usage(player, e.Limit);
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
            KeyUsage u = Usage(player, e != null ? e.Limit : null);
            u.Activated++;
            if (e != null && e.Limit != null && e.Limit.Oath) u.Used++;
            TurnEvents.Add("체인 " + chainLink + ": " + Who(player) + " " + Name(card) + " " + EffectName(e) + " 발동");
        }

        public void OnChainNegated(int chainLink)
        {
            PendingLink l;
            if (!_chain.TryGetValue(chainLink, out l)) return;
            l.ActivationNegated = true;
            Usage(l.Player, l.Effect != null ? l.Effect.Limit : null).ActivationNegated++;
            TurnEvents.Add("체인 " + chainLink + ": 발동 무효 (" + Name(l.Card) + ")");
        }

        public void OnChainDisabled(int chainLink)
        {
            PendingLink l;
            if (!_chain.TryGetValue(chainLink, out l)) return;
            l.EffectNegated = true;
            Usage(l.Player, l.Effect != null ? l.Effect.Limit : null).EffectNegated++;
            TurnEvents.Add("체인 " + chainLink + ": 효과 무효 (" + Name(l.Card) + ")");
        }

        public void OnChainSolved(int chainLink)
        {
            PendingLink l;
            if (!_chain.TryGetValue(chainLink, out l)) return;
            KeyUsage u = Usage(l.Player, l.Effect != null ? l.Effect.Limit : null);
            if (!l.ActivationNegated)
            {
                // "use" counts once the activation stands, even if the effect itself was negated.
                if (l.Effect == null || l.Effect.Limit == null || !l.Effect.Limit.Oath) u.Used++;
                if (!l.EffectNegated) u.Resolved++;
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
            KeyUsage u = Usage(0, proc.Limit);
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

        private static string Who(int player) { return player == 0 ? "자신" : "상대"; }
        private static string Name(CardEntry c) { return c == null ? "(알 수 없는 카드)" : c.Data.Name; }
        private static string EffectName(EffectEntry e) { return e == null ? "효과(미확인)" : "효과 " + e.Index; }

        public string UsageText(int player, EffectEntry e)
        {
            if (e.Limit == null && e.Kind.StartsWith("적용")) return "";
            KeyUsage u = Usage(player, e.Limit);
            string limit = e.Limit != null ? "/" + e.Limit.Count : "";
            return "발동 " + u.Activated + "회, 사용 " + u.Used + limit + (u.ActivationNegated > 0 ? ", 발동 무효 " + u.ActivationNegated : "")
                 + (u.EffectNegated > 0 ? ", 효과 무효 " + u.EffectNegated : "")
                 + (e.Limit != null && u.Used >= e.Limit.Count ? " → 이번 턴 더 못 씀" : "");
        }

        // ------------------------------------------------------------------ readable export

        public string Describe(Func<int, string> instances)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# 게임 카드 DB");
            sb.AppendLine();
            sb.AppendLine("- 턴: " + Turn + " / 이번 턴 일반 소환: " + (NormalSummonUsed ? "사용함" : "안 함"));
            sb.AppendLine("- 메인 덱 " + MainDeck.Count + "장, 엑스트라 덱 " + ExtraDeck.Count + "장, DB 카드 종류 " + Cards.Count);
            if (TurnEvents.Count > 0)
            {
                sb.AppendLine("- 이번 턴 기록:");
                foreach (string ev in TurnEvents) sb.AppendLine("  - " + ev);
            }
            sb.AppendLine();

            foreach (CardEntry c in Cards.Values.OrderBy(x => x.Opponent).ThenBy(x => x.InExtraDeck).ThenBy(x => x.Data.Name, StringComparer.Ordinal))
            {
                CardData d = c.Data;
                sb.AppendLine("## " + d.Name + " (" + d.Id + ")" + (c.Opponent ? " — 상대 카드" : c.CopiesInDeck > 0 ? " ×" + c.CopiesInDeck + (c.InExtraDeck ? " [엑스트라]" : "") : ""));
                sb.AppendLine(Stats(d));
                if (instances != null)
                {
                    string inst = instances(d.Id);
                    if (!String.IsNullOrEmpty(inst)) sb.AppendLine("- 지금: " + inst);
                }
                if (!c.ScriptFound) sb.AppendLine("- 스크립트 없음" + (c.Notes.Count > 0 ? " (" + String.Join(", ", c.Notes.ToArray()) + ")" : ""));
                if (c.Material != null)
                    sb.AppendLine("- 소환 조건: " + c.Material.Relation + FilterText(c.Material.Filter) + " → 이 덱에서: " + NamesOf(c.Material.Targets));

                foreach (EffectEntry e in c.Effects)
                {
                    sb.AppendLine("- 효과 " + e.Index + (e.DescIndex.HasValue ? " [설명 " + e.DescIndex + "]" : "") + ": " + e.Kind
                        + (e.Events.Count > 0 ? " / 계기: " + String.Join(", ", e.Events.ToArray()) : "")
                        + (e.Range != null ? " / 위치: " + e.Range : ""));
                    if (!String.IsNullOrEmpty(e.Label)) sb.AppendLine("  - 설명: " + e.Label);
                    if (e.Categories.Count > 0) sb.AppendLine("  - 분류: " + String.Join(", ", e.Categories.ToArray()));
                    if (e.Limit != null) sb.AppendLine("  - 횟수: 1턴 " + e.Limit.Count + "번" + (e.Limit.Oath ? " (OATH: 발동 시 소비)" : " (처리 시 소비)") + (e.Limit.PerDuel ? ", 듀얼 중" : "") + ", 키 " + e.Limit.Key);
                    foreach (CardLink l in e.Links)
                        sb.AppendLine("  - " + RoleKo(l.Role) + ": " + l.Relation + FilterText(l.Filter)
                            + (l.Opponent ? " → 상대 카드"
                               : l.Filter.Partial ? " → (미해석, 연결 계산 안 함)"
                               : l.AnyCard ? " → 조건에 맞는 아무 카드"
                               : " → 이 덱에서: " + NamesOf(l.Targets)));
                    string usage = UsageText(0, e);
                    if (usage.Length > 0) sb.AppendLine("  - 이번 턴(자신): " + usage);
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
