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

        /// <summary>
        /// A deck by name: the bot's Decks folder, then the game's deck folder (EDOPro's own decks), a full path, or ydke://.
        /// Null when not found or empty.
        /// </summary>
        public static Deck LoadDeck(string name)
        {
            if (String.IsNullOrEmpty(name)) return null;
            string path = null;
            if (name.StartsWith("ydke://")) path = name;
            else if (Path.IsPathRooted(name)) path = File.Exists(name) ? name : null;
            else
            {
                string file = name.EndsWith(".ydk", StringComparison.OrdinalIgnoreCase) ? name : name + ".ydk";
                foreach (string dir in new[] { Path.Combine(Program.AssetPath ?? ".", "Decks"), "deck", Path.Combine(Program.AssetPath ?? ".", "..", "deck") })
                {
                    string candidate = Path.GetFullPath(Path.Combine(dir, file));
                    if (File.Exists(candidate)) { path = candidate; break; }
                }
            }
            if (path == null) return null;
            try
            {
                Deck d = Deck.Load(path);
                return d != null && d.Cards.Count + d.ExtraCards.Count > 0 ? d : null;
            }
            catch (Exception) { return null; }
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
            // Optional: the opponent's deck, e.g. AgentOpponentDeck=AI_Ryzeal (as in the deck setting "Agent:AI_Ryzeal@AI_Ryzeal").
            Deck theirs = LoadDeck(Config.GetString("AgentOpponentDeck"));
            if (theirs != null) db.AddOpponentDeck(theirs.Cards, theirs.ExtraCards);
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
                // Optional GY and face-up spells for the test, e.g. AgentDbGrave=35844557,34022970 AgentDbSpells=6798031
                foreach (var extra in new[] { new KeyValuePair<string, string>("GRAVE", "AgentDbGrave"), new KeyValuePair<string, string>("SZONE", "AgentDbSpells"), new KeyValuePair<string, string>("MZONE", "AgentDbMonsters") })
                {
                    string list = Config.GetString(extra.Value);
                    if (list == null) continue;
                    foreach (string s in list.Split(','))
                    {
                        // Monsters may carry their materials: AgentDbMonsters=7511613:2,34909328:1
                        string[] parts = s.Trim().Split(':');
                        int id, mats;
                        if (!int.TryParse(parts[0], out id)) continue;
                        if (extra.Key == "MZONE" && parts.Length > 1 && int.TryParse(parts[1], out mats)) zones.AddMonster(id, mats);
                        else zones.Add(extra.Key, id);
                    }
                }
                zones.OpponentFaceUp = Config.GetInt("AgentDbOppFaceUp", -1); // -1: unknown
                // Breakthrough test: the opponent's monsters, e.g. AgentDbOppMonsters=34909328:a,9940036:d (a = attack, d = defense position)
                string oppMons = Config.GetString("AgentDbOppMonsters");
                if (oppMons != null)
                {
                    InterruptModel im = new InterruptModel(model.Weights);
                    foreach (string s in oppMons.Split(','))
                    {
                        string[] parts = s.Trim().Split(':');
                        int id;
                        if (!int.TryParse(parts[0], out id)) continue;
                        CardEntry oc = db.EnsureCard(id, true);
                        if (oc == null) continue;
                        OpponentMonster m = new OpponentMonster { Id = id, Atk = oc.Data.Atk, Def = oc.Data.Def, FaceUp = true, AttackPosition = parts.Length < 2 || parts[1] != "d" };
                        int v; // optional stats, e.g. 27204312:d:3000:5500 (a token)
                        if (parts.Length > 2 && int.TryParse(parts[2], out v)) m.Atk = v;
                        if (parts.Length > 3 && int.TryParse(parts[3], out v)) m.Def = v;
                        foreach (EffectEntry e in oc.Effects)
                            if (e.Range != null && e.Range.Contains("MZONE") && im.IsInterrupt(oc, e)) m.Held += im.Value(oc, e, false);
                        zones.OpponentMonsters.Add(m);
                    }
                    if (zones.OpponentFaceUp < 0) zones.OpponentFaceUp = zones.OpponentMonsters.Count;
                }
                zones.CanAttack = Config.GetBool("AgentDbCanAttack", oppMons != null);
                zones.OpponentLp = Config.GetInt("AgentDbOppLp", 8000);
                sb.AppendLine("# 가치 계산 시험 (패: " + hand + ", 묘지: " + (Config.GetString("AgentDbGrave") ?? "없음") + ", 앞면 마법: " + (Config.GetString("AgentDbSpells") ?? "없음") + ", 일반 소환 가능)");
                sb.AppendLine();
                sb.Append(DescribeValues(db, model, zones));
                sb.AppendLine("## 턴 계획 (흐름 + 턴 종료 방해 − 효율)");
                TurnPlanner planner = new TurnPlanner(db, model.Weights);
                planner.NextTurnValue = id => model.ValueNextTurn(id, zones).Value;
                sb.Append(DescribePlan(planner, zones));
                sb.AppendLine();
                sb.AppendLine("## 덱에 대상이 없어 0점인 효과 (서치·특수 소환·세트·덱에서 사용)");
                Dictionary<int, int> left = model.DeckRemaining(zones);
                Func<int, int> deckLeft = id => { int n; return left.TryGetValue(id, out n) ? n : 0; };
                int blocked = 0;
                foreach (CardEntry c in db.Cards.Values.Where(x => !x.Opponent))
                    foreach (EffectEntry e in c.Effects)
                    {
                        bool all = ValueModel.DeckBlocked(e, deckLeft, model.Weights.Excluded);
                        List<string> parts = e.Links.Where(l => ValueModel.DeckDepleted(l, deckLeft, model.Weights.Excluded))
                                                    .Select(l => l.Relation).ToList();
                        if (!all && parts.Count == 0) continue;
                        blocked++;
                        sb.AppendLine("- " + c.Data.Name + " 효과 " + e.Index + ": " + (all ? "효과 전체 0" : "일부 0") + " (" + String.Join(", ", parts.ToArray()) + ")");
                    }
                if (blocked == 0) sb.AppendLine("- 없음");
                sb.AppendLine();
                sb.AppendLine("## 카드 가치 (카드 선택 시, 덱 잔량 반영)");
                ZoneSnapshot snap = zones.Snapshot();
                foreach (CardEntry c in db.Cards.Values.Where(x => !x.Opponent))
                    sb.AppendLine("- " + c.Data.Name + ": " + planner.Worth(snap, c.Data.Id).ToString("0.#"));
                sb.AppendLine();
                sb.AppendLine("## 엑스트라 덱 코스트 순서 (낮을수록 먼저 묘지로, 상대 앞면 " + snap.OpponentFaceUp + "장)");
                foreach (CardEntry c in db.Cards.Values.Where(x => !x.Opponent && x.InExtraDeck).OrderBy(x => planner.FodderCost(snap, x.Data.Id)))
                    sb.AppendLine("- " + c.Data.Name + ": " + planner.FodderCost(snap, c.Data.Id).ToString("0.##"));
                sb.AppendLine();
            }
            if (Config.GetBool("AgentDbInterrupts", hand != null))
            {
                sb.AppendLine("# 방해 가치 (이 덱)");
                sb.AppendLine();
                sb.Append(DescribeInterrupts(db, LoadWeights()));
            }
            if (Config.GetString("AgentOpponentDeck") != null)
            {
                // Bait test: the opponent's whole deck unseen, AgentDbOppHand (default 5) + AgentDbOppSet cards usable now.
                ValueWeights w = LoadWeights();
                OpponentPool pool = new OpponentPool();
                int oppHand = Config.GetInt("AgentDbOppHand", 5), oppSet = Config.GetInt("AgentDbOppSet", 0);
                pool.Hidden = db.Cards.Values.Where(x => !db.IsExtraDeckCard(x.Data)).Sum(x => x.OpponentCopies);
                pool.Hand = oppHand;
                pool.Set = oppSet;
                pool.TheirTurn = Config.GetBool("AgentDbOppTurn", true);
                sb.AppendLine("# 미끼 시험 (상대 숨은 카드 " + pool.Hidden + "장, 손패 " + pool.Hand + "장, 세트 " + pool.Set + "장, 본 카드 없음)");
                sb.AppendLine();
                sb.Append(DescribeBait(db, w, pool, Config.GetString("AgentDbBait")));
            }
            string oppAct = Config.GetString("AgentDbOppActivate");
            if (oppAct != null && db.HasOpponentDeck) sb.Append(DescribeHitLoss(db, LoadWeights(), oppAct));
            File.WriteAllText(outPath, sb.ToString(), new UTF8Encoding(false));
            Logger.WriteLine("Card DB: " + db.Cards.Count + " cards, constants " + db.ConstantCount + ", script dirs " + db.ScriptDirCount + " → " + Path.GetFullPath(outPath));
        }

        /// <summary>
        /// Stop chance of each of our interruptions that is activated (a chain link), and, for AgentDbBait=id:effect,id:effect,...,
        /// the bait order of that set offered together.
        /// </summary>
        /// <summary>
        /// Hit-loss test (docs/INTERRUPTION_RISK.md 8.1): the opponent activates AgentDbOppActivate=id:effectIndex with their
        /// board AgentDbOppField (ids, monsters and spells, e.g. 8633261) / AgentDbOppGrave, Normal Summon spent unless
        /// AgentDbOppNormalSummon=true. Our answer AgentDbHitter=id[:effectIndex] (default: a plain negation).
        /// Prints the loss for 0..7 unknown cards in their hand.
        /// </summary>
        public static string DescribeHitLoss(GameCardDb db, ValueWeights w, string act)
        {
            StringBuilder sb = new StringBuilder();
            string[] p = act.Split(':');
            int id, idx = 0;
            if (!int.TryParse(p[0], out id) || (p.Length > 1 && !int.TryParse(p[1], out idx))) return "# 상대 손실 시험: AgentDbOppActivate 형식 오류 (id:효과번호)\n";
            CardEntry c = db.EnsureCard(id, true);
            EffectEntry e = c != null ? c.Effects.FirstOrDefault(x => x.Index == idx) : null;
            if (e == null) return "# 상대 손실 시험: " + act + " 효과 없음\n";

            StaticZoneView z = new StaticZoneView { NormalSummonAvailable = Config.GetBool("AgentDbOppNormalSummon", false) };
            OpponentPool seen = new OpponentPool();
            foreach (var kv in new[] { new KeyValuePair<string, string>("MZONE", "AgentDbOppField"), new KeyValuePair<string, string>("GRAVE", "AgentDbOppGrave") })
            {
                string list = Config.GetString(kv.Value);
                if (list == null) continue;
                foreach (string s in list.Split(','))
                {
                    string[] parts = s.Trim().Split(':');
                    int x, mats;
                    if (!int.TryParse(parts[0], out x)) continue;
                    CardEntry xc = db.EnsureCard(x, true);
                    bool spell = xc != null && (xc.Data.Type & 0x1) == 0;
                    if (kv.Key == "MZONE" && !spell && parts.Length > 1 && int.TryParse(parts[1], out mats)) z.AddMonster(x, mats); // Xyz with materials
                    else z.Add(kv.Key == "MZONE" && spell ? "SZONE" : kv.Key, x);
                    int n;
                    seen.Seen[x] = seen.Seen.TryGetValue(x, out n) ? n + 1 : 1;
                }
            }

            // Effects the opponent already used this turn, e.g. AgentDbOppUsed=8633261:2,35844557:2 (counted as activated and resolved).
            string used = Config.GetString("AgentDbOppUsed");
            if (used != null)
            {
                int link = 0;
                foreach (string s in used.Split(','))
                {
                    string[] up = s.Trim().Split(':');
                    int uid, uidx;
                    if (up.Length < 2 || !int.TryParse(up[0], out uid) || !int.TryParse(up[1], out uidx)) continue;
                    CardEntry uc = db.EnsureCard(uid, true);
                    EffectEntry ue = uc != null ? uc.Effects.FirstOrDefault(x => x.Index == uidx) : null;
                    if (ue == null) continue;
                    db.OnChaining(++link, 1, uid, ue.DescIndex.HasValue ? ((long)uid << 20) | (long)ue.DescIndex.Value : 0);
                    db.OnChainSolved(link);
                }
                db.OnChainEnd();
            }

            HitKind kind = HitKind.Negate;
            string hitter = Config.GetString("AgentDbHitter");
            string hitterName = "무효(가정)";
            if (hitter != null)
            {
                string[] hp = hitter.Split(':');
                int hid, hidx = -1;
                CardEntry hc = int.TryParse(hp[0], out hid) ? db.EnsureCard(hid, false) : null;
                if (hp.Length > 1) int.TryParse(hp[1], out hidx);
                EffectEntry he = hc == null ? null : hidx >= 0 ? hc.Effects.FirstOrDefault(x => x.Index == hidx)
                               : hc.Effects.FirstOrDefault(x => db.CanHit(hc, x, c, e) != HitKind.None);
                kind = he != null ? db.CanHit(hc, he, c, e) : HitKind.None;
                hitterName = hc != null ? hc.Data.Name + (he != null ? " 효과 " + he.Index : "") : hitter;
            }

            OpponentLossModel model = new OpponentLossModel(db, w);
            int deck = db.OpponentMainDeck.Count;
            int seenCount = seen.Seen.Values.Sum();
            sb.AppendLine("# 상대 손실 시험 (상대 발동: " + c.Data.Name + " 효과 " + e.Index + ", 우리 응답: " + hitterName + " → " + kind
                        + ", 상대 필드·묘지: " + seenCount + "장, 일반 소환 " + (z.NormalSummonAvailable ? "가능" : "사용함") + ")");
            sb.AppendLine();
            HitLoss first = model.Loss(c, e, kind, z, new OpponentPool { Hidden = deck - seenCount, Hand = 0 });
            sb.AppendLine("- 연쇄(맞지 않으면 상대가 얻는 값): " + first.Path.Describe());
            sb.AppendLine();
            sb.AppendLine("| 상대 모르는 손패 | 연쇄 | 재시작 기대값 | 손실 | 메모 |");
            sb.AppendLine("|---|---|---|---|---|");
            for (int h = 0; h <= 7; ++h)
            {
                OpponentPool pool = new OpponentPool { Hidden = deck - seenCount, Hand = h };
                foreach (var kv in seen.Seen) pool.Seen[kv.Key] = kv.Value;
                HitLoss r = model.Loss(c, e, kind, z, pool);
                sb.AppendLine("| " + h + " | " + r.Chain.ToString("0.#") + " | " + r.Restart.ToString("0.#") + " | " + r.Loss.ToString("0.#") + " | " + String.Join("; ", r.Notes.ToArray()) + " |");
            }
            sb.AppendLine();
            return sb.ToString();
        }

        public static string DescribeBait(GameCardDb db, ValueWeights w, OpponentPool pool, string set)
        {
            InterruptModel im = new InterruptModel(w);
            BaitModel bait = new BaitModel(db, w);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("| 카드 | 효과 | 가치(필드) | 막힐 확률 | 막을 수 있는 상대 카드 |");
            sb.AppendLine("|---|---|---|---|---|");
            foreach (CardEntry c in db.Cards.Values.Where(x => !x.Opponent && x.CopiesInDeck > 0).OrderBy(x => x.Data.Name, StringComparer.Ordinal))
                foreach (EffectEntry e in c.Effects)
                {
                    if (!e.Activates || !im.IsInterrupt(c, e) || im.TypeValue(c, e) <= 0) continue;
                    double p = bait.StopChance(pool, new KeyValuePair<CardEntry, EffectEntry>(c, e));
                    sb.AppendLine("| " + c.Data.Name + " | " + e.Index + " | " + im.Value(c, e, false).ToString("0.#") + " | " + p.ToString("0.##")
                        + " | " + String.Join(", ", db.Hitters(c, e).Where(x => x.Contains("(무효")).ToArray()) + " |");
                }
            sb.AppendLine();
            if (set == null) return sb.ToString();

            List<KeyValuePair<CardEntry, EffectEntry>> fx = new List<KeyValuePair<CardEntry, EffectEntry>>();
            List<double> values = new List<double>();
            foreach (string item in set.Split(','))
            {
                string[] kv = item.Split(':');
                int id, idx;
                if (kv.Length != 2 || !int.TryParse(kv[0], out id) || !int.TryParse(kv[1], out idx)) continue;
                CardEntry c = db.Get(id);
                EffectEntry e = c != null ? c.Effects.FirstOrDefault(x => x.Index == idx) : null;
                if (e == null) continue;
                fx.Add(new KeyValuePair<CardEntry, EffectEntry>(c, e));
                values.Add(im.Value(c, e, (e.Range != null && e.Range.Contains("HAND")) || c.TrapActInHand));
            }
            if (fx.Count < 2) return sb.ToString();
            List<string> notes = new List<string>();
            List<double> est = bait.Order(pool, fx, values, notes);
            sb.AppendLine("## 함께 제시된 방해의 순서 (높을수록 먼저)");
            foreach (int i in Enumerable.Range(0, fx.Count).OrderByDescending(i => est[i]))
                sb.AppendLine("- " + fx[i].Key.Data.Name + " 효과 " + fx[i].Value.Index + ": " + est[i].ToString("0.#") + " (" + notes[i] + ")");
            sb.AppendLine();
            return sb.ToString();
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
                ChainResult next = model.ValueNextTurn(c.Data.Id, zones);
                if (next.Steps.Count > 0)
                {
                    int n = zones.Hand(c.Data.Id);
                    double follow = 0;
                    for (int k = 1; k <= n; ++k) follow += ValueModel.FollowOf(model.Weights, next.Value, k);
                    sb.AppendLine("  - 다음 턴 후속" + (n > 1 ? " (" + n + "장)" : "") + ": " + follow.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)
                                + " = " + model.Weights.BaseOf("follow.discount") + " × " + next.Describe());
                }
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

        public StaticZoneView() { NormalSummonAvailable = true; OpponentFaceUp = -1; }
        public int Materials { get; set; }
        public int FieldMaterials() { return Materials + _monsWithMats.Sum(kv => kv.Value); } // loose materials + those of monsters added with a count
        public int OpponentFaceUp { get; set; }
        public bool CanAttack { get; set; }
        public int OpponentLp { get; set; }
        public readonly List<OpponentMonster> OpponentMonsters = new List<OpponentMonster>();
        private readonly List<KeyValuePair<int, int>> _monsWithMats = new List<KeyValuePair<int, int>>();

        /// <summary>A monster with its own material count (also counted in the MZONE zone).</summary>
        public void AddMonster(int id, int mats)
        {
            _monsWithMats.Add(new KeyValuePair<int, int>(id, mats));
            Add("MZONE", id);
        }

        public ZoneSnapshot Snapshot()
        {
            ZoneSnapshot z = new ZoneSnapshot { NormalSummonAvailable = NormalSummonAvailable, OpponentFaceUp = OpponentFaceUp, CanAttack = CanAttack, OpponentLp = OpponentLp > 0 ? OpponentLp : 8000 };
            z.OpponentMonsters.AddRange(OpponentMonsters);
            Fill(z.Hand, "HAND");
            List<int> mons = new List<int>();
            Fill(mons, "MZONE");
            foreach (var kv in _monsWithMats) mons.Remove(kv.Key);
            z.Monsters.AddRange(_monsWithMats);
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
