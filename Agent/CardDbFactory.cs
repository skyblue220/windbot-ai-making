using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WindBot.Agent.Knowledge;
using WindBot.Game;

namespace WindBot.Agent
{
    /// <summary>Finds the engine's script folders and databases, and builds a GameCardDb for a deck.</summary>
    public static class CardDbFactory
    {
        public static List<string> ScriptRoots()
        {
            // EDOPro starts the bot with the ProjectIgnis folder as working directory and AssetPath=./WindBot.
            List<string> roots = new List<string>();
            foreach (string baseDir in new[] { ".", "..", Program.AssetPath ?? "", Path.Combine(Program.AssetPath ?? "", "..") })
            {
                string script = Path.GetFullPath(Path.Combine(baseDir, "script"));
                if (Directory.Exists(script) && !roots.Contains(script)) roots.Add(script);
            }
            // Downloaded card repositories (e.g. repositories/delta-bagooska/script) hold scripts of newer cards.
            foreach (string baseDir in new[] { ".", "..", Path.Combine(Program.AssetPath ?? "", "..") })
            {
                string repos = Path.GetFullPath(Path.Combine(baseDir, "repositories"));
                if (!Directory.Exists(repos)) continue;
                foreach (string repo in Directory.GetDirectories(repos))
                {
                    string script = Path.Combine(repo, "script");
                    if (Directory.Exists(script) && !roots.Contains(script)) roots.Add(script);
                }
            }
            return roots;
        }

        public static GameCardDb Create()
        {
            List<string> roots = ScriptRoots();
            List<string> scriptDirs = new List<string>();
            List<string> constantFiles = new List<string>();
            foreach (string root in roots)
            {
                scriptDirs.Add(Path.Combine(root, "official"));
                scriptDirs.Add(root);
                constantFiles.Add(Path.Combine(root, "constant.lua"));
                constantFiles.Add(Path.Combine(root, "archetype_setcode_constants.lua"));
            }
            LuaConstants constants = LuaConstants.Load(constantFiles);
            return new GameCardDb(Program.LoadedDatabasePaths, scriptDirs, constants);
        }

        public static GameCardDb Build(Deck deck)
        {
            GameCardDb db = Create();
            db.BuildForDeck(deck.Cards, deck.ExtraCards);
            return db;
        }

        /// <summary>agent_values.json next to the bot (AssetPath). Created with the default base table if missing, so it can be edited.</summary>
        public static ValueWeights LoadWeights()
        {
            string path = Path.Combine(Program.AssetPath ?? ".", "agent_values.json");
            if (!File.Exists(path))
            {
                try { File.WriteAllText(path, new ValueWeights().ToJson(), new UTF8Encoding(false)); }
                catch (Exception ex) { Logger.WriteErrorLine("Could not write " + path + ": " + ex.Message); }
            }
            return ValueWeights.Load(path);
        }

        public static void Dump(string deckName, string outPath)
        {
            Deck deck = Deck.Load(deckName);
            GameCardDb db = Build(deck);
            StringBuilder sb = new StringBuilder(db.Describe(null));

            // Optional: chain values for a hypothetical opening hand, e.g. AgentDbHand=8633261,35844557
            string hand = Config.GetString("AgentDbHand");
            if (hand != null)
            {
                ValueModel model = new ValueModel(db, LoadWeights());
                StaticZoneView zones = new StaticZoneView();
                foreach (string s in hand.Split(','))
                {
                    int id;
                    if (int.TryParse(s.Trim(), out id)) zones.Add("HAND", id);
                }
                sb.AppendLine("# 가치 계산 시험 (패: " + hand + ", 필드·묘지 비어 있음, 일반 소환 가능)");
                sb.AppendLine();
                sb.Append(DescribeValues(db, model, zones));
                sb.AppendLine("## 턴 계획 (흐름 + 턴 종료 방해 − 효율)");
                sb.Append(DescribePlan(new TurnPlanner(db, model.Weights), zones));
                sb.AppendLine();
            }
            if (Config.GetBool("AgentDbInterrupts", hand != null))
            {
                sb.AppendLine("# 방해 가치 (이 덱)");
                sb.AppendLine();
                sb.Append(DescribeInterrupts(db, LoadWeights()));
            }
            File.WriteAllText(outPath, sb.ToString(), new UTF8Encoding(false));
            Logger.WriteLine("Card DB: " + db.Cards.Count + " cards, constants " + db.ConstantCount + ", script dirs " + db.ScriptDirCount + " → " + Path.GetFullPath(outPath));
        }

        /// <summary>Interruption value of every effect in the deck that can be used on the opponent's turn.</summary>
        public static string DescribeInterrupts(GameCardDb db, ValueWeights w)
        {
            InterruptModel im = new InterruptModel(w);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("| 카드 | 효과 | 종류 | 패에서 | 필드·세트 |");
            sb.AppendLine("|---|---|---|---|---|");
            foreach (CardEntry c in db.Cards.Values.Where(x => !x.Opponent).OrderBy(x => x.Data.Name, StringComparer.Ordinal))
                foreach (EffectEntry e in c.Effects)
                {
                    if (!im.IsInterrupt(c, e) || im.TypeValue(c, e) <= 0) continue;
                    bool hand = (e.Range != null && e.Range.Contains("HAND")) || c.TrapActInHand;
                    sb.AppendLine("| " + c.Data.Name + (w.Excluded.Contains(c.Data.Id) ? " (제외)" : "") + " | " + e.Index + " | " + im.TypeName(c, e)
                        + " | " + (hand ? im.Value(c, e, true).ToString("0.#") : "-") + " | " + im.Value(c, e, false).ToString("0.#") + " |");
                }
            return sb.ToString();
        }

        /// <summary>Best plan for this turn and the best plan for each first move.</summary>
        public static string DescribePlan(TurnPlanner planner, IZoneView zones)
        {
            StringBuilder sb = new StringBuilder();
            List<PlanResult> byFirst;
            PlanResult stop;
            PlanResult best = planner.Plan(zones.Snapshot(), out byFirst, out stop);
            sb.AppendLine("- 최선 계획: " + best.Describe());
            sb.AppendLine("- 아무것도 안 하고 턴 종료: " + stop.Describe());
            foreach (PlanResult r in byFirst.GroupBy(x => x.Moves[0].Text).Select(g => g.OrderByDescending(x => x.Value).First())
                                            .OrderByDescending(x => x.Value).Take(10))
                sb.AppendLine("  - 첫 행동 " + r.Moves[0].Text + " → " + r.Value.ToString("0.#"));
            if (planner.Exhausted) sb.AppendLine("- (탐색 예산 소진: 일부 경로만 확인)");
            return sb.ToString();
        }

        /// <summary>Readable value table for the cards in hand: deck remaining, best chain if brought out, normal summon chain.</summary>
        public static string DescribeValues(GameCardDb db, ValueModel model, IZoneView zones)
        {
            StringBuilder sb = new StringBuilder();
            Dictionary<int, int> left = model.DeckRemaining(zones);
            List<string> deck = new List<string>();
            foreach (var kv in left)
                if (kv.Value > 0) deck.Add(db.Get(kv.Key).Data.Name + " " + kv.Value);
            sb.AppendLine("- 덱에 남은 카드(덱 매수 − 보이는 장수): " + String.Join(", ", deck.ToArray()));
            sb.AppendLine("- γ = " + model.Weights.Gamma + ", 가중치 출처: " + model.Weights.Source);
            foreach (CardEntry c in db.Cards.Values)
            {
                if (zones.Hand(c.Data.Id) <= 0) continue;
                ChainResult best = model.ValueInHand(c.Data.Id, zones);
                ChainResult ns = model.ValueOfNormalSummon(c.Data.Id, zones);
                sb.AppendLine("- 패: " + c.Data.Name + " → 최선 " + best.Describe());
                if (ns.Steps.Count > 0) sb.AppendLine("  - 일반 소환 시: " + ns.Describe());
            }
            sb.AppendLine();
            return sb.ToString();
        }
    }

    /// <summary>Zone counts set by hand (offline tests).</summary>
    public sealed class StaticZoneView : IZoneView
    {
        private readonly Dictionary<string, Dictionary<int, int>> _zones = new Dictionary<string, Dictionary<int, int>>();
        public bool NormalSummonAvailable { get; set; }

        public StaticZoneView() { NormalSummonAvailable = true; }
        public int Materials { get; set; }
        public int FieldMaterials() { return Materials; }

        public ZoneSnapshot Snapshot()
        {
            ZoneSnapshot z = new ZoneSnapshot { NormalSummonAvailable = NormalSummonAvailable };
            Fill(z.Hand, "HAND");
            List<int> mons = new List<int>();
            Fill(mons, "MZONE");
            int mats = Materials;
            foreach (int id in mons) { z.Monsters.Add(new KeyValuePair<int, int>(id, mats)); mats = 0; }
            Fill(z.Spells, "SZONE");
            Fill(z.Grave, "GRAVE");
            Fill(z.Banished, "REMOVED");
            return z;
        }

        private void Fill(List<int> list, string zone)
        {
            Dictionary<int, int> z;
            if (!_zones.TryGetValue(zone, out z)) return;
            foreach (var kv in z) for (int i = 0; i < kv.Value; ++i) list.Add(kv.Key);
        }

        public void Add(string zone, int id)
        {
            Dictionary<int, int> z;
            if (!_zones.TryGetValue(zone, out z)) { z = new Dictionary<int, int>(); _zones[zone] = z; }
            int n;
            z.TryGetValue(id, out n);
            z[id] = n + 1;
        }

        private int Get(string zone, int id)
        {
            Dictionary<int, int> z;
            int n;
            return _zones.TryGetValue(zone, out z) && z.TryGetValue(id, out n) ? n : 0;
        }

        public int Hand(int id) { return Get("HAND", id); }
        public int Field(int id) { return Get("MZONE", id) + Get("SZONE", id); }
        public int Grave(int id) { return Get("GRAVE", id); }
        public int Banished(int id) { return Get("REMOVED", id); }

        public bool AnyOwn(string zones, Func<int, bool> match)
        {
            foreach (string zone in ZoneNames(zones))
            {
                Dictionary<int, int> z;
                if (_zones.TryGetValue(zone, out z))
                    foreach (var kv in z) if (kv.Value > 0 && match(kv.Key)) return true;
            }
            return false;
        }

        public static IEnumerable<string> ZoneNames(string zones)
        {
            foreach (string p in zones.Split('|'))
            {
                if (p == "ONFIELD") { yield return "MZONE"; yield return "SZONE"; }
                else yield return p;
            }
        }
    }
}
