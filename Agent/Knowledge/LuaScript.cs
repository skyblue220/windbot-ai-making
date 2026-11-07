using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace WindBot.Agent.Knowledge
{
    /// <summary>Numeric constants from the engine's Lua headers (constant.lua, archetype_setcode_constants.lua).</summary>
    public sealed class LuaConstants
    {
        private static readonly Regex Assign = new Regex(@"^\s*([A-Z][A-Z0-9_]*)\s*=\s*([^-\r\n]+)", RegexOptions.Compiled);
        private readonly Dictionary<string, long> _values = new Dictionary<string, long>(StringComparer.Ordinal);

        public int Count { get { return _values.Count; } }

        public static LuaConstants Load(IEnumerable<string> files)
        {
            LuaConstants c = new LuaConstants();
            List<KeyValuePair<string, string>> pending = new List<KeyValuePair<string, string>>();
            foreach (string file in files)
            {
                if (!File.Exists(file)) continue;
                foreach (string raw in File.ReadAllLines(file))
                {
                    Match m = Assign.Match(raw);
                    if (m.Success) pending.Add(new KeyValuePair<string, string>(m.Groups[1].Value, m.Groups[2].Value.Trim()));
                }
            }
            // Resolve in passes so constants defined from other constants work.
            for (int pass = 0; pass < 4; ++pass)
                foreach (KeyValuePair<string, string> kv in pending)
                {
                    long v;
                    if (!c._values.ContainsKey(kv.Key) && c.TryEval(kv.Value, 0, out v)) c._values[kv.Key] = v;
                }
            return c;
        }

        public bool TryGet(string name, out long value)
        {
            return _values.TryGetValue(name, out value);
        }

        /// <summary>Evaluates "A|B", "A+B", numbers and the script-local "id".</summary>
        public bool TryEval(string expr, long selfId, out long value)
        {
            value = 0;
            if (String.IsNullOrEmpty(expr)) return false;
            string e = expr.Trim();
            if (e.StartsWith("(") && e.EndsWith(")")) e = e.Substring(1, e.Length - 2);
            foreach (string part in e.Split('|', '+'))
            {
                string t = part.Trim();
                if (t.Length == 0) return false;
                long v;
                if (t == "id") v = selfId;
                else if (t.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    if (!long.TryParse(t.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out v)) return false;
                }
                else if (!long.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out v) && !_values.TryGetValue(t, out v)) return false;
                value |= v;
            }
            return true;
        }
    }

    public sealed class EffectDef
    {
        public string Var;
        public int Order;
        public string ClonedFrom;
        public int? DescIndex;
        public string TypeExpr;
        public string CodeExpr;
        public string CategoryExpr;
        public string RangeExpr;
        public string ValueExpr;
        public string PropertyExpr;
        public string CountLimitExpr;
        public string Condition;
        public string Cost;
        public string Target;
        public string Operation;
        public bool Registered;

        public EffectDef Clone(string newVar, int order)
        {
            EffectDef e = (EffectDef)MemberwiseClone();
            e.Var = newVar;
            e.Order = order;
            e.ClonedFrom = Var;
            e.Registered = false;
            return e;
        }
    }

    public sealed class MaterialDef
    {
        /// <summary>"Xyz" or "Link".</summary>
        public string Kind;
        public string FilterExpr;
        public int Level;
        public int Min;
        public int Max;
    }

    public sealed class Selection
    {
        public string Function;
        public string Call;
        public string Filter;
        public string SelfLocation;
        public string OpponentLocation;
        /// <summary>Written as "not Duel.IsExisting...": the cards must NOT exist.</summary>
        public bool Negated;
        /// <summary>Position of the call in the function body (to find the action that uses the picked cards).</summary>
        public int Index;

        /// <summary>Calls that only check existence/count rather than pick cards to act on.</summary>
        public bool IsCheck
        {
            get { return Call == "IsExistingMatchingCard" || Call == "IsExistingTarget" || Call == "GetMatchingGroupCount"; }
        }
    }

    public sealed class Predicate
    {
        public string Method;
        public List<string> Args = new List<string>();
        public bool Negated;
        public override string ToString()
        {
            return (Negated ? "not " : "") + Method + "(" + String.Join(",", Args.ToArray()) + ")";
        }
    }

    /// <summary>"(c:IsA(x) or c:IsB(y))" — true when any item is true; Negated for "not (...)".</summary>
    public sealed class PredicateGroup
    {
        public readonly List<Predicate> Items = new List<Predicate>();
        public bool Negated;
        public override string ToString()
        {
            return (Negated ? "not " : "") + "(" + String.Join(" or ", Items.Select(i => i.ToString()).ToArray()) + ")";
        }
    }

    public sealed class FilterInfo
    {
        public readonly List<Predicate> Predicates = new List<Predicate>();
        public readonly List<PredicateGroup> Groups = new List<PredicateGroup>();
        /// <summary>True when the filter has structure we do not evaluate (or / not(...) / unknown calls).</summary>
        public bool Partial;
        public string Raw;
    }

    /// <summary>
    /// Mechanical reading of one card script: effects registered in initial_effect, summon
    /// procedures, function bodies, selections and filter predicates. It does not run Lua.
    /// </summary>
    public sealed class LuaScript
    {
        private static readonly Regex FuncStart = new Regex(@"^function\s+s\.(\w+)\s*\(", RegexOptions.Compiled);
        private static readonly Regex CreateEffect = new Regex(@"^\s*(?:local\s+)?(\w+)\s*=\s*Effect\.CreateEffect\(", RegexOptions.Compiled);
        private static readonly Regex CloneEffect = new Regex(@"^\s*(?:local\s+)?(\w+)\s*=\s*(\w+):Clone\(\)", RegexOptions.Compiled);
        private static readonly Regex SetCall = new Regex(@"^\s*(\w+):Set(\w+)\((.*)\)\s*$", RegexOptions.Compiled);
        private static readonly Regex Register = new Regex(@"(?:\w+:|Duel\.)RegisterEffect\(\s*(\w+)", RegexOptions.Compiled);
        private static readonly Regex XyzProc = new Regex(@"Xyz\.AddProcedure\(\s*c\s*,(.*)\)\s*$", RegexOptions.Compiled);
        private static readonly Regex LinkProc = new Regex(@"Link\.AddProcedure\(\s*c\s*,(.*)\)\s*$", RegexOptions.Compiled);
        private static readonly Regex StringId = new Regex(@"aux\.Stringid\(\s*id\s*,\s*(\d+)\s*\)", RegexOptions.Compiled);
        private static readonly Regex FuncRef = new Regex(@"^s\.(\w+)$", RegexOptions.Compiled);
        private static readonly Regex SelectWithPlayer = new Regex(
            @"Duel\.(SelectMatchingCard|SelectTarget|SelectReleaseGroup)\(\s*\w+\s*,\s*([\w\.]+(?:\([^()]*\))?)\s*,\s*\w+\s*,\s*([A-Z_|]+|0)\s*,\s*([A-Z_|]+|0)",
            RegexOptions.Compiled);
        private static readonly Regex SelectNoPlayer = new Regex(
            @"Duel\.(GetMatchingGroup|GetMatchingGroupCount|IsExistingMatchingCard|IsExistingTarget|GetFirstMatchingCard)\(\s*([\w\.]+(?:\([^()]*\))?)\s*,\s*\w+\s*,\s*([A-Z_|]+|0)\s*,\s*([A-Z_|]+|0)",
            RegexOptions.Compiled);
        private static readonly Regex DuelCall = new Regex(@"Duel\.(\w+)\(", RegexOptions.Compiled);
        private static readonly Regex PredicateCall = new Regex(@"(not\s+)?c:(Is\w+)\(([^()]*)\)", RegexOptions.Compiled);
        private static readonly Regex NestedFilter = new Regex(@"s\.(\w+)\(\s*c\b", RegexOptions.Compiled);
        private static readonly Regex OrGroup = new Regex(@"(not\s+)?\(\s*((?:(?:not\s+)?c:Is\w+\([^()]*\)\s*(?:or\s+)?)+)\)", RegexOptions.Compiled);
        private static readonly Regex BoolFunction = new Regex(@"aux\.FilterBoolFunctionEx?\(\s*Card\.(Is\w+)\s*,\s*([^()]*)\)", RegexOptions.Compiled);

        public bool Found;
        public string Path;
        public readonly Dictionary<string, string> Functions = new Dictionary<string, string>(StringComparer.Ordinal);
        public readonly List<EffectDef> Effects = new List<EffectDef>();
        public MaterialDef Material;
        public readonly List<string> Notes = new List<string>();

        public static LuaScript Load(int cardId, IEnumerable<string> scriptDirs)
        {
            LuaScript s = new LuaScript();
            foreach (string dir in scriptDirs)
            {
                string p = System.IO.Path.Combine(dir, "c" + cardId.ToString(CultureInfo.InvariantCulture) + ".lua");
                if (!File.Exists(p)) continue;
                s.Found = true;
                s.Path = p;
                s.Parse(File.ReadAllLines(p));
                break;
            }
            return s;
        }

        private static string StripComment(string line)
        {
            int i = line.IndexOf("--", StringComparison.Ordinal);
            return i >= 0 ? line.Substring(0, i) : line;
        }

        private void Parse(string[] lines)
        {
            // 1) split top-level functions
            string current = null;
            StringBuilder body = null;
            foreach (string raw in lines)
            {
                string line = StripComment(raw);
                Match fm = FuncStart.Match(line);
                if (current == null && fm.Success)
                {
                    current = fm.Groups[1].Value;
                    body = new StringBuilder();
                    continue;
                }
                if (current != null)
                {
                    if (line.StartsWith("end") && line.Trim() == "end")
                    {
                        Functions[current] = body.ToString();
                        current = null;
                        continue;
                    }
                    body.AppendLine(line);
                }
            }
            if (current != null) Functions[current] = body.ToString();

            // 2) effects registered in initial_effect
            string init;
            if (!Functions.TryGetValue("initial_effect", out init))
            {
                Notes.Add("initial_effect not found");
                return;
            }
            Dictionary<string, EffectDef> byVar = new Dictionary<string, EffectDef>(StringComparer.Ordinal);
            int order = 0;
            foreach (string line in init.Split('\n'))
            {
                Match m;
                if ((m = CreateEffect.Match(line)).Success)
                {
                    EffectDef e = new EffectDef();
                    e.Var = m.Groups[1].Value;
                    e.Order = ++order;
                    byVar[e.Var] = e;
                    Effects.Add(e);
                }
                else if ((m = CloneEffect.Match(line)).Success && byVar.ContainsKey(m.Groups[2].Value))
                {
                    EffectDef e = byVar[m.Groups[2].Value].Clone(m.Groups[1].Value, ++order);
                    byVar[e.Var] = e;
                    Effects.Add(e);
                }
                else if ((m = SetCall.Match(line)).Success && byVar.ContainsKey(m.Groups[1].Value))
                {
                    Apply(byVar[m.Groups[1].Value], m.Groups[2].Value, m.Groups[3].Value.Trim());
                }
                else if ((m = XyzProc.Match(line)).Success)
                {
                    List<string> a = SplitArgs(m.Groups[1].Value);
                    Material = new MaterialDef { Kind = "Xyz", FilterExpr = a.Count > 0 ? a[0] : "nil" };
                    if (a.Count > 1) int.TryParse(a[1], out Material.Level);
                    if (a.Count > 2) int.TryParse(a[2], out Material.Min);
                    Material.Max = Material.Min;
                    if (a.Count > 6 && a[6].Contains("InfiniteMats")) Material.Max = 99;
                    else if (a.Count > 5) { int mx; if (int.TryParse(a[5], out mx)) Material.Max = mx; }
                }
                else if ((m = LinkProc.Match(line)).Success)
                {
                    List<string> a = SplitArgs(m.Groups[1].Value);
                    Material = new MaterialDef { Kind = "Link", FilterExpr = a.Count > 0 ? a[0] : "nil" };
                    if (a.Count > 1) int.TryParse(a[1], out Material.Min);
                    if (a.Count > 2) int.TryParse(a[2], out Material.Max);
                }
                foreach (Match r in Register.Matches(line))
                    if (byVar.ContainsKey(r.Groups[1].Value)) byVar[r.Groups[1].Value].Registered = true;
            }
            Effects.RemoveAll(e => !e.Registered);
            if (init.Contains(".AddProcedure(") && Material == null) Notes.Add("summon procedure not parsed");
        }

        private static void Apply(EffectDef e, string field, string args)
        {
            switch (field)
            {
                case "Description":
                    Match m = StringId.Match(args);
                    if (m.Success) e.DescIndex = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                    break;
                case "Type": e.TypeExpr = args; break;
                case "Code": e.CodeExpr = args; break;
                case "Category": e.CategoryExpr = args; break;
                case "Range": e.RangeExpr = args; break;
                case "Value": e.ValueExpr = args; break;
                case "Property": e.PropertyExpr = args; break;
                case "CountLimit": e.CountLimitExpr = args; break;
                case "Condition": e.Condition = FuncName(args); break;
                case "Cost": e.Cost = FuncName(args); break;
                case "Target": e.Target = FuncName(args); break;
                case "Operation": e.Operation = FuncName(args); break;
            }
        }

        private static string FuncName(string arg)
        {
            Match m = FuncRef.Match(arg.Trim());
            return m.Success ? m.Groups[1].Value : "(inline) " + arg.Trim();
        }

        /// <summary>Splits on top-level commas, respecting (), {} nesting.</summary>
        public static List<string> SplitArgs(string s)
        {
            List<string> parts = new List<string>();
            int depth = 0;
            StringBuilder cur = new StringBuilder();
            foreach (char ch in s)
            {
                if (ch == '(' || ch == '{') depth++;
                if (ch == ')' || ch == '}') depth--;
                if (ch == ',' && depth == 0) { parts.Add(cur.ToString().Trim()); cur.Length = 0; continue; }
                cur.Append(ch);
            }
            if (cur.Length > 0) parts.Add(cur.ToString().Trim());
            return parts;
        }

        public string Body(string function)
        {
            string b;
            return function != null && Functions.TryGetValue(function, out b) ? b : null;
        }

        /// <summary>Body of a function plus bodies of non-filter s.* helpers it calls (one level).</summary>
        public string ExpandedBody(string function)
        {
            string b = Body(function);
            if (b == null) return null;
            StringBuilder sb = new StringBuilder(b);
            foreach (Match m in Regex.Matches(b, @"s\.(\w+)\(\s*(e|tp|c)\b"))
            {
                string inner = Body(m.Groups[1].Value);
                if (inner != null && !inner.TrimStart().StartsWith("return c:")) sb.AppendLine(inner);
            }
            return sb.ToString();
        }

        public List<Selection> Selections(string function)
        {
            List<Selection> result = new List<Selection>();
            string body = Body(function);
            if (body == null) return result;
            foreach (Regex rx in new[] { SelectWithPlayer, SelectNoPlayer })
                foreach (Match m in rx.Matches(body))
                {
                    int from = Math.Max(0, m.Index - 6);
                    bool negated = body.Substring(from, m.Index - from).Contains("not");
                    result.Add(new Selection
                    {
                        Function = function, Call = m.Groups[1].Value, Filter = m.Groups[2].Value,
                        SelfLocation = m.Groups[3].Value, OpponentLocation = m.Groups[4].Value, Negated = negated, Index = m.Index
                    });
                }
            result.Sort((a, b) => a.Index.CompareTo(b.Index));
            return result;
        }

        private static readonly HashSet<string> ActionNames = new HashSet<string>
        {
            "SpecialSummon", "SendtoHand", "Overlay", "SSet", "Destroy", "SendtoGrave", "SendtoDeck", "Remove", "Release", "Draw",
            "ConfirmCards", "XyzSummon", "SynchroSummon", "LinkSummon"
        };

        // Duel.RemoveOverlayCard(tp, s, o, min, max, reason) / Duel.CheckRemoveOverlayCard(tp, s, o, min, reason)
        private static readonly Regex DetachCall = new Regex(@"Duel\.(?:Check)?RemoveOverlayCard\(\s*\w+\s*,\s*\d+\s*,\s*\d+\s*,\s*(\d+)", RegexOptions.Compiled);
        // c:RemoveOverlayCard(tp, min, max, reason) / c:CheckRemoveOverlayCard(tp, min, reason), e.g. Rafflesia's copy effect in script/official
        private static readonly Regex CardDetachCall = new Regex(@":(?:Check)?RemoveOverlayCard\(\s*\w+\s*,\s*(\d+)", RegexOptions.Compiled);
        // Cost.Detach(n) / Cost.DetachFromSelf(n), also inside Cost.AND(...) (delta scripts)
        private static readonly Regex CostDetach = new Regex(@"Cost\.Detach(?:FromSelf)?\(\s*(\d+)", RegexOptions.Compiled);

        /// <summary>Xyz materials the effect removes (as cost or as part of resolution), 0 when none.</summary>
        public int DetachCount(params string[] functions)
        {
            int n = 0;
            foreach (string f in functions)
            {
                if (f == null) continue;
                if (f.StartsWith("(inline)"))
                {
                    Match c = CostDetach.Match(f);
                    if (c.Success) n = Math.Max(n, int.Parse(c.Groups[1].Value, CultureInfo.InvariantCulture));
                    continue;
                }
                string body = ExpandedBody(f);
                if (body == null) continue;
                foreach (Regex rx in new[] { DetachCall, CardDetachCall, CostDetach })
                    foreach (Match m in rx.Matches(body))
                        n = Math.Max(n, int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture));
            }
            return n;
        }

        /// <summary>Card-moving Duel calls in a function body (not expanded), in order of appearance.</summary>
        public List<KeyValuePair<int, string>> ActionCalls(string function)
        {
            List<KeyValuePair<int, string>> calls = new List<KeyValuePair<int, string>>();
            string body = Body(function);
            if (body == null) return calls;
            foreach (Match m in DuelCall.Matches(body))
                if (ActionNames.Contains(m.Groups[1].Value)) calls.Add(new KeyValuePair<int, string>(m.Index, m.Groups[1].Value));
            return calls;
        }

        public List<string> DuelCalls(string function)
        {
            List<string> calls = new List<string>();
            string body = ExpandedBody(function);
            if (body == null) return calls;
            foreach (Match m in DuelCall.Matches(body))
                if (!calls.Contains(m.Groups[1].Value)) calls.Add(m.Groups[1].Value);
            return calls;
        }

        public bool Mentions(string function, string token)
        {
            string body = ExpandedBody(function);
            return body != null && body.Contains(token);
        }

        /// <summary>Static reading of a filter: "s.name", "aux.FilterBoolFunctionEx(Card.IsX,ARG)" or "nil".</summary>
        public FilterInfo ReadFilter(string filterExpr)
        {
            FilterInfo info = new FilterInfo();
            info.Raw = filterExpr;
            if (String.IsNullOrEmpty(filterExpr) || filterExpr == "nil" || filterExpr == "aux.TRUE") return info;
            // aux.NecroValleyFilter(f) only adds a field-spell check; the card condition is f.
            Match nv = Regex.Match(filterExpr, @"^aux\.NecroValleyFilter\((.+)\)$");
            if (nv.Success) filterExpr = nv.Groups[1].Value.Trim();
            if (filterExpr.StartsWith("Card.Is"))
            {
                info.Predicates.Add(new Predicate { Method = filterExpr.Substring(5) });
                return info;
            }
            // aux.FaceupFilter(Card.IsX, ARG) = face-up and IsX(ARG)
            Match ff = Regex.Match(filterExpr, @"^aux\.FaceupFilter\(\s*Card\.(Is\w+)\s*,\s*([^()]*)\)$");
            if (ff.Success)
            {
                info.Predicates.Add(new Predicate { Method = "IsFaceup" });
                Predicate p = new Predicate { Method = ff.Groups[1].Value };
                foreach (string a in SplitArgs(ff.Groups[2].Value)) p.Args.Add(a);
                info.Predicates.Add(p);
                return info;
            }
            Match bf = BoolFunction.Match(filterExpr);
            if (bf.Success)
            {
                Predicate p = new Predicate { Method = bf.Groups[1].Value };
                foreach (string a in SplitArgs(bf.Groups[2].Value)) p.Args.Add(a);
                info.Predicates.Add(p);
                return info;
            }
            Match fr = FuncRef.Match(filterExpr.Trim());
            if (!fr.Success)
            {
                info.Partial = true;
                return info;
            }
            CollectPredicates(fr.Groups[1].Value, info, 0);
            return info;
        }

        private void CollectPredicates(string function, FilterInfo info, int depth)
        {
            string body = Body(function);
            if (body == null || depth > 3) { info.Partial = true; return; }
            int ret = body.IndexOf("return", StringComparison.Ordinal);
            if (ret < 0) { info.Partial = true; return; }
            string expr = body.Substring(ret + 6);
            info.Raw = (info.Raw == null ? "" : info.Raw + " ; ") + Regex.Replace(expr.Trim(), @"\s+", " ");
            // "(c:IsA(x) or c:IsB(y))" and "not (...)" groups are read as OR groups; other "or" stays unparsed.
            foreach (Match g in OrGroup.Matches(expr))
            {
                PredicateGroup group = new PredicateGroup { Negated = g.Groups[1].Success };
                foreach (Match m in PredicateCall.Matches(g.Groups[2].Value))
                {
                    Predicate p = new Predicate { Method = m.Groups[2].Value, Negated = m.Groups[1].Success };
                    foreach (string a in SplitArgs(m.Groups[3].Value)) if (a.Length > 0) p.Args.Add(a);
                    group.Items.Add(p);
                }
                info.Groups.Add(group);
            }
            expr = OrGroup.Replace(expr, " ");
            if (Regex.IsMatch(expr, @"\bor\b") || expr.Contains("not (") || Regex.IsMatch(body, @"\b(local|if)\b")) info.Partial = true;
            foreach (Match m in PredicateCall.Matches(expr))
            {
                Predicate p = new Predicate { Method = m.Groups[2].Value, Negated = m.Groups[1].Success };
                foreach (string a in SplitArgs(m.Groups[3].Value)) if (a.Length > 0) p.Args.Add(a);
                info.Predicates.Add(p);
            }
            foreach (Match m in NestedFilter.Matches(expr))
                if (m.Groups[1].Value != function) CollectPredicates(m.Groups[1].Value, info, depth + 1);
        }
    }
}
