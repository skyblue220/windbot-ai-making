using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace WindBot.Agent.Knowledge
{
    /// <summary>
    /// Base values per effect function (shared by every theme, fixed) plus learned per-card offsets.
    /// Loaded from a flat JSON file so learning never touches code. See VALUE_MODEL.md.
    /// </summary>
    public sealed class ValueWeights
    {
        public double Gamma = 0.7;
        /// <summary>Hard safety cap on chain steps. The real length comes from resources.</summary>
        public int MaxSteps = 12;

        public readonly Dictionary<string, double> Base = new Dictionary<string, double>(StringComparer.Ordinal)
        {
            { "special_summon_from_deck", 40 },
            { "special_summon_from_grave", 35 },
            { "search", 30 },
            { "set_from_deck", 30 },
            { "draw", 25 },
            { "rule_special_summon", 20 },
            { "extra_normal_summon", 20 }, // provisional: a summon from hand that keeps the turn's Normal Summon (e.g. Sakitama)
            { "grave_to_deck", 10 },
            { "attach_from_grave", 10 },
            { "attach_from_deck", 10 }, // not in the agreed table yet: same as grave → material for now
            { "extra_to_grave", 10 },
            { "destroy_replace", 15 },
            { "destroy_or_negate", 35 },
            { "cost_extra_to_grave", 0 }, // plus grave_fodder × the sent card's "sent to the GY" effect (e.g. Eclipse Twins ③, Aggregator ③)
            { "grave_fodder", 0.5 },     // that effect still needs the card attached (Detonator ②) and detached on the opponent's turn
            { "cost_detach", -2 }, // materials are dummies; their worth is what they fuel (VALUE_MODEL.md 2장)

            // Interruptions on the opponent's turn (VALUE_MODEL.md 7.2 / 7.3)
            { "int.destroy", 30 },
            { "int.banish", 30 },
            { "int.bounce_hand", 30 },   // return to hand (Extra Deck monsters go back to the Extra Deck): no GY effect left behind
            { "int.bounce_deck", 35 },   // shuffle into the deck: like banish
            { "int.control", 45 },       // take control: removes their card and adds a body to our field
            { "int.reactive_factor", 0.67 }, // destroy / banish usable only when the opponent activates: that effect still resolves (30 → 20)
            { "int.expected_targets", 2 },   // opponent face-up cards assumed for "up to N" removal when their field is not yet known
            { "int.negate_effect", 40 },
            { "int.negate_activation", 38 },
            { "int.negate_effect_destroy", 48 },
            { "int.negate_activation_destroy", 47 },
            { "int.draw", 60 },
            { "int.wipe", 50 },
            { "int.protect", 30 },
            { "int.lock", 35 },
            { "int.handtrap_bonus", 15 },
            { "int.chainproof_bonus", 5 },
            { "int.battle_factor", 0.5 },
            { "battle.destroy", 30 },    // destroying a monster by battle, plus the interruptions it holds on the field
            { "battle.lp", 100 },        // battle damage: this much for 8000 LP (lethal damage always wins)
        };

        /// <summary>
        /// Per card-effect interruption settings, key "&lt;kind&gt;.&lt;cardId&gt;:&lt;effectIndex&gt;":
        /// ib = base override, is = scope factor, ibt = battle-phase only (1), ia = additive bonus,
        /// imc = multiply by the number of our monsters matching the effect's condition (1).
        /// Defaults below are the agreed AI_Ryzeal table (VALUE_MODEL.md 7.4); other cards fall back to
        /// automatic classification from the Lua categories.
        /// </summary>
        public readonly Dictionary<string, double> CardInt = new Dictionary<string, double>(StringComparer.Ordinal)
        {
            { "is.14558127:1", 0.8 },                         // Ash Blossom: limited trigger list
            { "ib.42141493:1", 50 },                          // Mulcharmy Fuwalos: draw, needs empty field
            { "ib.84192580:1", 50 }, { "is.84192580:1", 0.8 }, // Mulcharmy Purulia
            { "is.73642296:1", 0.5 },                         // Ghost Belle: GY effects only
            { "is.67750322:1", 0.5 },                         // Skull Meister: opponent GY only
            { "ib.27204311:1", 50 }, { "is.27204311:1", 0.5 }, // Nibiru: wipe, 5-summon condition
            { "is.31548215:1", 0.8 },                         // Gravedigger's Trap Hole
            { "ib.65681983:1", 40 }, { "is.65681983:1", 0.8 }, // Crossout Designator: negate by name
            { "imc.33787730:1", 1 },                          // Plasma Hole: up to the number of Ryzeal Xyz
            { "ib.6798031:4", 40 }, { "is.6798031:4", 0.8 }, { "ia.6798031:4", 5 }, // Ryzeal Cross ③: does not activate
            { "ib.6511113:5", 55 }, { "is.6511113:5", 1.0 }, { "ia.6511113:5", 10 }, // Rafflesia ③: copies a Hole trap (negate); hard to remove
            { "is.9940036:3", 0.8 },                          // Aggregator ③
            { "ib.93039339:1", 35 }, { "is.93039339:1", 0.5 }, // TY-PHON ①: lock
            { "is.6983839:1", 0.5 },                          // Tornado Dragon: S/T only
            { "ib.2061963:1", 38 }, { "is.2061963:1", 0.8 }, { "ibt.2061963:1", 1 }, // Number 104
            { "ib.46772449:1", 50 }, { "is.46772449:1", 0.5 }, { "ibt.46772449:1", 1 }, // Exciton Knight
            { "ib.1269512:2", 40 }, { "is.1269512:2", 0.5 }, { "ibt.1269512:2", 1 },   // FA Ray Lancer ②
        };

        public bool TryCardInt(string kind, int cardId, int effectIndex, out double v)
        {
            return CardInt.TryGetValue(kind + "." + cardId.ToString(CultureInfo.InvariantCulture) + ":" + effectIndex, out v);
        }

        /// <summary>Penalty per hand card spent, for the end-of-turn interruption evaluation (VALUE_MODEL.md 7.5). Provisional; not used until that evaluation exists.</summary>
        public double EfficiencyPerHandCard = 10;

        /// <summary>Probability that the value policy picks a random legal answer instead (exploration for learning).</summary>
        public double Explore = 0; // random steps lose real duels; turn on only for controlled experiments

        /// <summary>Cards left out of every evaluation (e.g. put in the deck by mistake). Key in the file: "exclude.&lt;cardId&gt;": 1.</summary>
        public readonly HashSet<int> Excluded = new HashSet<int>();

        /// <summary>Learned offsets, key "cardId:effectIndex". Learning changes only these.</summary>
        public readonly Dictionary<string, double> Offsets = new Dictionary<string, double>(StringComparer.Ordinal);

        public string Source = "defaults";

        public ValueWeights()
        {
            // Put in AI_Ryzeal by mistake (2026-11-07); excluded until removed from the ydk. Overridable in the file with value 0.
        }

        public double BaseOf(string function)
        {
            double v;
            return function != null && Base.TryGetValue(function, out v) ? v : 0;
        }

        public double OffsetOf(int cardId, int effectIndex)
        {
            double v;
            return Offsets.TryGetValue(cardId.ToString(CultureInfo.InvariantCulture) + ":" + effectIndex, out v) ? v : 0;
        }

        private static readonly Regex Pair = new Regex("\"([^\"]+)\"\\s*:\\s*(-?[0-9]+(?:\\.[0-9]+)?)", RegexOptions.Compiled);

        /// <summary>
        /// Flat JSON: {"gamma":0.7, "maxSteps":12, "base.search":30, ..., "offset.34022970:2":5}.
        /// Missing keys keep their defaults.
        /// </summary>
        public static ValueWeights Load(string path)
        {
            ValueWeights w = new ValueWeights();
            if (path == null || !File.Exists(path)) return w;
            foreach (Match m in Pair.Matches(File.ReadAllText(path)))
            {
                string key = m.Groups[1].Value;
                double v = double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                if (key == "gamma") w.Gamma = v;
                else if (key == "maxSteps") w.MaxSteps = (int)v;
                else if (key == "efficiencyPerHandCard") w.EfficiencyPerHandCard = v;
                else if (key == "explore") w.Explore = v;
                else if (key.StartsWith("exclude."))
                {
                    int id;
                    if (int.TryParse(key.Substring(8), out id)) { if (v != 0) w.Excluded.Add(id); else w.Excluded.Remove(id); }
                }
                else if (key.StartsWith("base.")) w.Base[key.Substring(5)] = v;
                else if (key.StartsWith("ib.") || key.StartsWith("is.") || key.StartsWith("ibt.") || key.StartsWith("ia.") || key.StartsWith("imc.")) w.CardInt[key] = v;
                else if (key.StartsWith("offset.")) w.Offsets[key.Substring(7)] = v;
            }
            w.Source = path;
            return w;
        }

        public string ToJson()
        {
            StringBuilder sb = new StringBuilder("{\n");
            sb.Append("  \"gamma\": ").Append(Gamma.ToString(CultureInfo.InvariantCulture)).Append(",\n");
            sb.Append("  \"maxSteps\": ").Append(MaxSteps.ToString(CultureInfo.InvariantCulture)).Append(",\n");
            sb.Append("  \"efficiencyPerHandCard\": ").Append(EfficiencyPerHandCard.ToString(CultureInfo.InvariantCulture)).Append(",\n");
            sb.Append("  \"explore\": ").Append(Explore.ToString(CultureInfo.InvariantCulture));
            foreach (int id in Excluded) sb.Append(",\n  \"exclude.").Append(id.ToString(CultureInfo.InvariantCulture)).Append("\": 1");
            foreach (var kv in Base) sb.Append(",\n  \"base.").Append(kv.Key).Append("\": ").Append(kv.Value.ToString(CultureInfo.InvariantCulture));
            foreach (var kv in CardInt) sb.Append(",\n  \"").Append(kv.Key).Append("\": ").Append(kv.Value.ToString(CultureInfo.InvariantCulture));
            foreach (var kv in Offsets) sb.Append(",\n  \"offset.").Append(kv.Key).Append("\": ").Append(kv.Value.ToString(CultureInfo.InvariantCulture));
            return sb.Append("\n}\n").ToString();
        }
    }

    /// <summary>Where our own copies of a card are right now. Deck contents are hidden, so the deck count is derived.</summary>
    public interface IZoneView
    {
        /// <summary>Own copies in hand / field (monster + spell zones) / grave / banished.</summary>
        int Hand(int cardId);
        int Field(int cardId);
        int Grave(int cardId);
        int Banished(int cardId);
        bool NormalSummonAvailable { get; }
        /// <summary>Total Xyz materials attached to our monsters.</summary>
        int FieldMaterials();
        /// <summary>All our visible cards, for the turn planner.</summary>
        ZoneSnapshot Snapshot();
        /// <summary>Any own card in the given zones matches the predicate (used for summon conditions).</summary>
        bool AnyOwn(string zones, Func<int, bool> match);
    }

    public sealed class ChainStep
    {
        public string Card;
        public int EffectIndex;
        public string Function;
        public double Base;
        public double Discounted;
    }

    public sealed class ChainResult
    {
        public double Value;
        public readonly List<ChainStep> Steps = new List<ChainStep>();

        public string Describe()
        {
            if (Steps.Count == 0) return "0";
            return Value.ToString("0.#", CultureInfo.InvariantCulture) + " = "
                 + String.Join(" → ", Steps.Select(s => s.Card + "(" + s.EffectIndex + ") " + s.Function + " " + s.Base.ToString("0.#", CultureInfo.InvariantCulture)
                                                     + (s.Discounted != s.Base ? "×γ^" + Steps.IndexOf(s) : "")).ToArray());
        }
    }

    /// <summary>
    /// Finite geometric-series value of what a card or effect brings this turn:
    ///   S = Σ_{k=0}^{n} γ^k × (base_k + offset_k)
    /// The length n is not a fixed depth: the chain stops when the next card is no longer in the deck
    /// (copies in deck − copies seen in hand/field/grave/banished, decremented along the path),
    /// when the next effect's once-per-turn key is already used (this turn or earlier on the path),
    /// or when the arrival event does not trigger it (normal vs special summon, summon right).
    /// </summary>
    public sealed class ValueModel
    {
        private readonly GameCardDb _db;
        private readonly ValueWeights _w;

        public ValueModel(GameCardDb db, ValueWeights weights)
        {
            _db = db;
            _w = weights;
        }

        public ValueWeights Weights { get { return _w; } }

        private sealed class Ctx
        {
            public IZoneView Zones;
            public Dictionary<int, int> DeckLeft = new Dictionary<int, int>();
            public HashSet<string> UsedKeys = new HashSet<string>(StringComparer.Ordinal);
            /// <summary>Cards put on the field earlier on this path (for summon conditions further down the chain).</summary>
            public List<int> OnField = new List<int>();
            public bool NormalSummon;
            public int Budget = 20000;
            public int Left(int id) { int n; return DeckLeft.TryGetValue(id, out n) ? n : 0; }

            public Ctx Clone()
            {
                Ctx c = new Ctx { Zones = Zones, NormalSummon = NormalSummon, Budget = Budget };
                foreach (var kv in DeckLeft) c.DeckLeft[kv.Key] = kv.Value;
                foreach (string k in UsedKeys) c.UsedKeys.Add(k);
                c.OnField.AddRange(OnField);
                return c;
            }
        }

        /// <summary>Copies of each main-deck card still in the deck: deck list count − copies seen elsewhere.</summary>
        public Dictionary<int, int> DeckRemaining(IZoneView z)
        {
            Dictionary<int, int> left = new Dictionary<int, int>();
            foreach (int id in _db.MainDeck.Distinct())
            {
                int n = _db.MainDeck.Count(x => x == id) - z.Hand(id) - z.Field(id) - z.Grave(id) - z.Banished(id);
                left[id] = Math.Max(0, n);
            }
            return left;
        }

        private Ctx NewCtx(IZoneView z)
        {
            Ctx c = new Ctx { Zones = z, NormalSummon = z.NormalSummonAvailable };
            foreach (var kv in DeckRemaining(z)) c.DeckLeft[kv.Key] = kv.Value;
            // Effects whose once-per-turn count is already spent this turn.
            foreach (CardEntry card in _db.Cards.Values)
                if (!card.Opponent) // our own usage only
                foreach (EffectEntry e in card.Effects)
                {
                    KeyUsage u = _db.PeekUsage(0, e);
                    if (u != null && e.Limit != null && u.Used >= e.Limit.Count) c.UsedKeys.Add(e.UsageKey);
                }
            return c;
        }

        // ------------------------------------------------------------------ public entry points

        /// <summary>Value of using a specific effect of a card now (an activation option).</summary>
        public ChainResult ValueOfEffect(int cardId, EffectEntry effect, IZoneView z)
        {
            CardEntry card = _db.Get(cardId);
            if (card == null || effect == null) return new ChainResult();
            return FromEffect(card, effect, NewCtx(z), 0);
        }

        /// <summary>Value of a card in hand: best way to bring it out (rule summon or normal summon) and what follows.</summary>
        public ChainResult ValueInHand(int cardId, IZoneView z)
        {
            CardEntry card = _db.Get(cardId);
            if (card == null) return new ChainResult();
            return FromHand(card, NewCtx(z), 0);
        }

        /// <summary>Value of normal summoning a card from hand now.</summary>
        public ChainResult ValueOfNormalSummon(int cardId, IZoneView z)
        {
            CardEntry card = _db.Get(cardId);
            Ctx ctx = NewCtx(z);
            if (card == null || !ctx.NormalSummon) return new ChainResult();
            ctx.NormalSummon = false;
            return Arrive(card, "SUMMON_SUCCESS", ctx, 0);
        }

        // ------------------------------------------------------------------ recursion over the card network

        private ChainResult FromHand(CardEntry card, Ctx ctx, int step, bool noNormalSummon = false)
        {
            ChainResult best = new ChainResult();
            if (step >= _w.MaxSteps || --ctx.Budget < 0) return best;
            if (_w.Excluded.Contains(card.Data.Id)) return best;

            // Spells in hand: their activation effects. (Traps must be set first, so they bring nothing this turn.)
            if ((card.Data.Type & 0x2) != 0)
            {
                foreach (EffectEntry e in card.Effects)
                {
                    if (!e.Kind.StartsWith("카드의 발동") || ctx.UsedKeys.Contains(e.UsageKey)) continue;
                    ChainResult r = FromEffect(card, e, ctx.Clone(), step);
                    if (r.Value > best.Value) best = r;
                }
                return best;
            }

            // Rule special summon from hand (its own once-per-turn key, condition must hold).
            EffectEntry proc = card.Effects.FirstOrDefault(e => e.Kind.StartsWith("룰") && (e.Range == null || e.Range.Contains("HAND")));
            if (proc != null && !ctx.UsedKeys.Contains(proc.UsageKey) && ConditionsHold(card, proc, ctx))
            {
                ChainResult r = FromEffect(card, proc, ctx.Clone(), step);
                if (r.Value > best.Value) best = r;
            }
            // Normal summon (level 4 or lower monsters without tribute), consumes the summon right.
            if (ctx.NormalSummon && !noNormalSummon && IsMonster(card) && card.Data.Level <= 4 && !IsExtraType(card))
            {
                Ctx c = ctx.Clone();
                c.NormalSummon = false;
                ChainResult r = Arrive(card, "SUMMON_SUCCESS", c, step);
                if (r.Value > best.Value) best = r;
            }
            return best;
        }

        /// <summary>The card has just arrived on the field by the given event: take the best effect it can use next.</summary>
        private ChainResult Arrive(CardEntry card, string arrivalEvent, Ctx ctx, int step)
        {
            ChainResult best = new ChainResult();
            if (step >= _w.MaxSteps || --ctx.Budget < 0) return best;
            if (_w.Excluded.Contains(card.Data.Id)) return best;
            ctx.OnField.Add(card.Data.Id);
            foreach (EffectEntry e in card.Effects)
            {
                bool triggered = e.Kind.StartsWith("유발 효과") && e.Events.Contains(arrivalEvent);
                bool ignitionOnField = e.Kind.StartsWith("기동") && (e.Range == null || e.Range.Contains("MZONE"));
                if (!triggered && !ignitionOnField) continue;
                if (ctx.UsedKeys.Contains(e.UsageKey)) continue;
                ChainResult r = FromEffect(card, e, ctx.Clone(), step);
                if (r.Value > best.Value) best = r;
            }
            return best;
        }

        /// <summary>Use effect e of card at chain position step: its own term plus the best continuation through its targets.</summary>
        private ChainResult FromEffect(CardEntry card, EffectEntry e, Ctx ctx, int step)
        {
            ChainResult result = new ChainResult();
            if (step >= _w.MaxSteps || --ctx.Budget < 0) return result;
            if (_w.Excluded.Contains(card.Data.Id)) return result;
            if (ctx.UsedKeys.Contains(e.UsageKey)) return result;
            if (!HasTargets(e, ctx)) return result; // e.g. "special summon from grave" with nothing in the grave: cannot be activated
            if (e.DetachCount > ctx.Zones.FieldMaterials()) return result; // e.g. Star's rule summon needs a material to detach
            ctx.UsedKeys.Add(e.UsageKey);

            double discount = Math.Pow(_w.Gamma, step);
            string function;
            double own = OwnValue(card, e, ctx, out function) + _w.OffsetOf(card.Data.Id, e.Index);

            // Best continuation among the cards this effect brings out.
            ChainResult bestNext = new ChainResult();
            if (e.Kind.StartsWith("룰"))
            {
                // A rule summon brings the card itself to the field: its "on special summon" effects come next.
                bestNext = Arrive(card, "SPSUMMON_SUCCESS", ctx.Clone(), step + 1);
            }
            foreach (CardLink l in e.Links)
            {
                if (l.Role != "effect" || l.Opponent || l.Targets.Count == 0) continue;
                string arrival = Arrival(l);
                if (arrival == null) continue;
                foreach (int t in l.Targets.Distinct())
                {
                    if (!Available(t, l, ctx)) continue;
                    CardEntry target = _db.Get(t);
                    Ctx c = ctx.Clone();
                    Take(t, l, c);
                    ChainResult next;
                    if (arrival == "HAND") next = FromHand(target, c, step + 1, e.BlocksNormalSummonOfAdded);
                    else if (arrival == "SET") next = new ChainResult(); // set spells/traps: no chain continuation modelled yet
                    else next = Arrive(target, arrival, c, step + 1);
                    if (next.Value > bestNext.Value) bestNext = next;
                }
            }

            result.Steps.Add(new ChainStep { Card = card.Data.Name, EffectIndex = e.Index, Function = function, Base = own, Discounted = own * discount });
            result.Steps.AddRange(bestNext.Steps);
            result.Value = own * discount + bestNext.Value;
            return result;
        }

        /// <summary>Base value of the effect itself (sum over what it does) minus costs.</summary>
        private double OwnValue(CardEntry card, EffectEntry e, Ctx ctx, out string function)
        {
            List<string> functions = new List<string>();
            double v = 0;
            if (e.Kind.StartsWith("룰")) { v += _w.BaseOf("rule_special_summon"); functions.Add("룰 특수 소환"); }
            if (e.Kind.Contains("DESTROY_REPLACE")) { v += _w.BaseOf("destroy_replace"); functions.Add("파괴 대체"); }
            HashSet<string> seen = new HashSet<string>();
            // Functions that bring a card (summon / search / set) are alternatives within one effect
            // ("A or B"), so only the best one counts. Side functions (attach, return to deck) add up.
            string bestBring = null;
            foreach (CardLink l in e.Links)
            {
                if (l.Role != "effect") continue;
                string f = FunctionOf(l);
                if (f == null || DeckDepleted(l, ctx.Left, _w.Excluded) || !seen.Add(f)) continue;
                if (Arrival(l) != null)
                {
                    if (bestBring == null || _w.BaseOf(f) > _w.BaseOf(bestBring)) bestBring = f;
                    continue;
                }
                v += _w.BaseOf(f);
                functions.Add(KoreanOf(f));
            }
            if (bestBring != null) { v += _w.BaseOf(bestBring); functions.Insert(0, KoreanOf(bestBring)); }
            else if (e.Categories.Contains("SEARCH") && seen.Add("search"))
            {
                // Adds a card to hand but the pick was not readable (e.g. excavate then add): count the search value, no chain.
                v += _w.BaseOf("search");
                functions.Insert(0, "서치(분류 기준)");
            }
            if (e.Categories.Contains("DRAW") && seen.Add("draw")) { v += _w.BaseOf("draw"); functions.Add("드로우"); }
            if ((e.Categories.Contains("DESTROY") || e.Categories.Contains("DISABLE") || e.Categories.Contains("NEGATE")) && seen.Add("destroy_or_negate"))
            { v += _w.BaseOf("destroy_or_negate"); functions.Add("파괴/무효"); }

            // Costs
            foreach (CardLink l in e.Links)
            {
                if (l.Role != "cost") continue;
                if (l.Relation.Contains("엑스트라 덱에서 묘지로")) { v += _w.BaseOf("cost_extra_to_grave"); functions.Add("코스트(엑스트라→묘지)"); }
                else if (l.Relation.Contains("묘지로")) { double c = CheapestFodder(card, ctx); v -= c; functions.Add("코스트(패/필드→묘지 " + c.ToString("0.#", CultureInfo.InvariantCulture) + ")"); }
            }
            if (e.DetachCount > 0) { v += e.DetachCount * _w.BaseOf("cost_detach"); functions.Add("소재 제거 " + e.DetachCount); }

            function = functions.Count > 0 ? String.Join("+", functions.ToArray()) : "기능 미분류";
            return v;
        }

        /// <summary>
        /// The link picks only from our own Deck, its candidates were read completely, and none of them is left there
        /// (deck list − copies seen elsewhere). Its search / summon / set / use-from-deck then does nothing: value 0.
        /// Unread or "any card" filters are never treated as empty.
        /// </summary>
        public static bool DeckDepleted(CardLink l, Func<int, int> deckLeft, ICollection<int> excluded)
        {
            if (l.Opponent || String.IsNullOrEmpty(l.Location) || !l.Location.Contains("DECK")) return false;
            if (l.Location.Contains("GRAVE") || l.Location.Contains("REMOVED") || l.Location.Contains("HAND") || l.Location.Contains("EXTRA")) return false;
            if (l.Filter == null || l.Filter.Partial || l.AnyCard || l.Targets.Count == 0) return false;
            return !l.Targets.Any(t => !excluded.Contains(t) && deckLeft(t) > 0);
        }

        /// <summary>
        /// The effect cannot do anything with what is left in the deck: a cost taken from the deck has no candidate,
        /// or every card it would bring (search / summon / set) comes only from the deck and none is left.
        /// </summary>
        public static bool DeckBlocked(EffectEntry e, Func<int, int> deckLeft, ICollection<int> excluded)
        {
            foreach (CardLink l in e.Links)
                if (l.Role == "cost" && DeckDepleted(l, deckLeft, excluded)) return true;
            List<CardLink> brings = e.Links.Where(l => l.Role == "effect" && !l.Opponent && Arrival(l) != null).ToList();
            return brings.Count > 0 && brings.All(l => DeckDepleted(l, deckLeft, excluded));
        }

        public static string FunctionOf(CardLink l)
        {
            string r = l.Relation;
            if (r.Contains("엑시즈 소재로"))
            {
                if (r.StartsWith("묘지")) return "attach_from_grave";
                if (r.StartsWith("덱에서")) return "attach_from_deck";
                return null;
            }
            if (r.Contains("특수 소환"))
            {
                if (r.StartsWith("덱에서")) return "special_summon_from_deck";
                if (r.Contains("묘지")) return "special_summon_from_grave";
                return null;
            }
            if (r.StartsWith("덱에서 패에 넣기")) return "search";
            if (r.StartsWith("덱에서 세트")) return "set_from_deck";
            if (r.StartsWith("묘지에서 덱으로")) return "grave_to_deck";
            if (r.StartsWith("엑스트라 덱에서 묘지로")) return "extra_to_grave";
            return null;
        }

        private static string KoreanOf(string f)
        {
            switch (f)
            {
                case "special_summon_from_deck": return "덱 특수 소환";
                case "special_summon_from_grave": return "묘지 특수 소환";
                case "search": return "서치";
                case "set_from_deck": return "덱 세트";
                case "grave_to_deck": return "묘지→덱";
                case "attach_from_grave": return "묘지→소재";
                case "attach_from_deck": return "덱→소재";
                case "extra_to_grave": return "엑스트라→묘지";
                default: return f;
            }
        }

        /// <summary>Where a target lands: field by special summon, hand, or set. Null when it does not continue a chain.</summary>
        private static string Arrival(CardLink l)
        {
            string f = FunctionOf(l);
            if (f == "special_summon_from_deck" || f == "special_summon_from_grave") return "SPSUMMON_SUCCESS";
            if (f == "search") return "HAND";
            if (f == "set_from_deck") return "SET";
            return null;
        }

        /// <summary>
        /// An effect whose main action picks from our deck/grave needs at least one such card left.
        /// Links we could not evaluate (unparsed or "any card") are assumed available.
        /// </summary>
        private bool HasTargets(EffectEntry e, Ctx ctx)
        {
            // If the effect brings a card (summon / search / set), that main pick decides whether it can be activated;
            // side functions (e.g. Plugin's "then attach from deck") do not.
            if (DeckBlocked(e, ctx.Left, _w.Excluded)) return false; // e.g. Rafflesia with no "Hole" trap left to send
            bool hasMain = e.Links.Any(l => l.Role == "effect" && !l.Opponent && Arrival(l) != null);
            bool needsTarget = false;
            foreach (CardLink l in e.Links)
            {
                if (l.Role != "effect" || l.Opponent || FunctionOf(l) == null) continue;
                if (hasMain && Arrival(l) == null) continue;
                if (l.Filter == null || l.Filter.Partial || l.AnyCard)
                {
                    // Unparsed target: from the deck we assume something fits; from grave/banishment there must be a card there at all.
                    if (l.Location.Contains("DECK")) return true;
                    if ((l.Location.Contains("GRAVE") || l.Location.Contains("REMOVED")) && ctx.Zones.AnyOwn(l.Location, id => true)) return true;
                    needsTarget = true;
                    continue;
                }
                needsTarget = true;
                foreach (int t in l.Targets.Distinct())
                    if (Available(t, l, ctx)) return true;
            }
            return !needsTarget;
        }

        private bool Available(int cardId, CardLink l, Ctx ctx)
        {
            if (_w.Excluded.Contains(cardId)) return false;
            if (l.Location.Contains("DECK"))
            {
                int left;
                return ctx.DeckLeft.TryGetValue(cardId, out left) && left > 0;
            }
            if (l.Location.Contains("GRAVE")) return ctx.Zones.Grave(cardId) > 0;
            return false;
        }

        private static void Take(int cardId, CardLink l, Ctx ctx)
        {
            if (l.Location.Contains("DECK") && ctx.DeckLeft.ContainsKey(cardId)) ctx.DeckLeft[cardId]--;
        }

        /// <summary>Existence conditions of a rule summon (e.g. "a Ryzeal monster on field or in grave"). Unparsed conditions count as satisfied.</summary>
        private bool ConditionsHold(CardEntry card, EffectEntry e, Ctx ctx)
        {
            foreach (CardLink l in e.Links)
            {
                if (l.Role != "condition" || l.Opponent || l.Filter == null || l.Filter.Partial || l.AnyCard) continue;
                HashSet<int> ok = new HashSet<int>(l.Targets);
                bool any = ctx.Zones.AnyOwn(l.Location, id => ok.Contains(id))
                        || ((l.Location.Contains("MZONE") || l.Location.Contains("ONFIELD")) && ctx.OnField.Any(ok.Contains));
                if (l.Relation.StartsWith("존재") && !any) return false;
                if (l.Relation.StartsWith("부재") && any) return false;
            }
            return true;
        }

        /// <summary>Cost of discarding: the cheapest other own card in hand/field, valued by its own effects only (no recursion).</summary>
        private double CheapestFodder(CardEntry self, Ctx ctx)
        {
            double best = double.MaxValue;
            foreach (CardEntry c in _db.Cards.Values)
            {
                if (c.Opponent) continue;
                int copies = ctx.Zones.Hand(c.Data.Id) + ctx.Zones.Field(c.Data.Id) - (c == self ? 1 : 0);
                if (copies <= 0) continue;
                double v = 0;
                foreach (EffectEntry e in c.Effects)
                {
                    if (ctx.UsedKeys.Contains(e.UsageKey)) continue;
                    string unused;
                    v += Math.Max(0, ShallowValue(c, e, ctx, out unused));
                }
                if (v < best) best = v;
            }
            return best == double.MaxValue ? 0 : best;
        }

        private double ShallowValue(CardEntry card, EffectEntry e, Ctx ctx, out string function)
        {
            List<string> functions = new List<string>();
            double v = 0;
            if (DeckBlocked(e, ctx.Left, _w.Excluded)) { function = "덱에 대상 없음"; return 0; }
            if (e.Kind.StartsWith("룰")) { v += _w.BaseOf("rule_special_summon"); functions.Add("rule"); }
            foreach (CardLink l in e.Links)
                if (l.Role == "effect" && !DeckDepleted(l, ctx.Left, _w.Excluded)) { string f = FunctionOf(l); if (f != null) { v += _w.BaseOf(f); functions.Add(f); } }
            // Hand traps and removal keep their interaction value even when discarded as a cost.
            if (e.Categories.Contains("DESTROY") || e.Categories.Contains("DISABLE") || e.Categories.Contains("NEGATE"))
            { v += _w.BaseOf("destroy_or_negate"); functions.Add("destroy_or_negate"); }
            function = String.Join("+", functions.ToArray());
            return v;
        }

        private bool IsMonster(CardEntry c) { return (c.Data.Type & 0x1) != 0; }
        private bool IsExtraType(CardEntry c) { return (c.Data.Type & (0x40 | 0x2000 | 0x800000 | 0x4000000)) != 0; } // fusion, synchro, xyz, link
    }
}
