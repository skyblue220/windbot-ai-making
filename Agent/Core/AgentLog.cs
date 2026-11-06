using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace WindBot.Agent.Core
{
    /// <summary>
    /// One JSONL file per duel. Event types:
    /// header, decision, message (engine notification), result, error.
    /// Dependency-free JSON writing so the core stays portable.
    /// </summary>
    public sealed class AgentLog
    {
        private readonly object _gate = new object();
        private readonly string _path;
        private int _sequence;

        public string Path { get { return _path; } }

        public AgentLog(string directory, string duelId)
        {
            if (String.IsNullOrEmpty(directory)) directory = ".";
            try { Directory.CreateDirectory(directory); }
            catch (Exception ex) { Console.Error.WriteLine("Agent log directory failed: " + ex.Message); }
            string file = "agent_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture) + "_" + duelId + ".jsonl";
            _path = System.IO.Path.Combine(directory, file);
        }

        public void Header(string duelId, string buildId, string policy, int seed, string deck)
        {
            Json j = Begin("header");
            j.Str("duelId", duelId).Str("buildId", buildId).Str("policy", policy).Num("seed", seed).Str("deck", deck);
            Write(j);
        }

        public void Decision(DecisionRequest request, StateSnapshot state, Decision decision, bool explored = false)
        {
            Json j = Begin("decision");
            j.Str("kind", request.Kind.ToString()).Str("shape", request.Shape.ToString()).Bool("explored", explored).Bool("preferLow", request.PreferLow)
             .Num("min", request.Min).Num("max", request.Max).Bool("cancelable", request.Cancelable)
             .Num("hint", request.Hint).Num("description", request.Description).Num("sourceCardId", request.SourceCardId);
            if (request.Shape == RequestShape.Sum) j.Num("target", request.Target).Bool("exactSum", request.ExactSum);
            if (request.Shape == RequestShape.Counter) j.Num("quantity", request.Quantity);
            j.Raw("options", OptionsJson(request.Options));
            j.Raw("state", StateJson(state));
            j.Raw("chosen", IntArray(decision.Indices)).Bool("canceled", decision.Canceled);
            Write(j);
        }

        public void Message(string message, int turn, string phase)
        {
            Json j = Begin("message");
            j.Str("message", message).Num("turn", turn).Str("phase", phase);
            Write(j);
        }

        public void Result(string result, int turn)
        {
            Json j = Begin("result");
            j.Str("result", result).Num("turn", turn);
            Write(j);
        }

        public void CardDb(string tag, string path, int cards)
        {
            Json j = Begin("carddb");
            j.Str("tag", tag).Str("path", path).Num("cards", cards);
            Write(j);
        }

        public void Error(string where, string detail)
        {
            Json j = Begin("error");
            j.Str("where", where).Str("detail", detail);
            Write(j);
        }

        private Json Begin(string type)
        {
            Json j = new Json();
            j.Str("type", type).Num("seq", ++_sequence)
             .Str("timestampUtc", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
            return j;
        }

        private void Write(Json j)
        {
            lock (_gate)
            {
                try { File.AppendAllText(_path, j.Close() + Environment.NewLine, Encoding.UTF8); }
                catch (Exception ex) { Console.Error.WriteLine("Agent log write failed: " + ex.Message); }
            }
        }

        private static string OptionsJson(List<DecisionOption> options)
        {
            StringBuilder sb = new StringBuilder("[");
            for (int i = 0; i < options.Count; ++i)
            {
                DecisionOption o = options[i];
                if (i > 0) sb.Append(',');
                Json j = new Json();
                j.Str("action", o.Action).Num("cardId", o.CardId).Num("controller", o.Controller)
                 .Num("location", o.Location).Num("sequence", o.Sequence).Num("position", o.Position)
                 .Num("description", o.Description).Num("param1", o.Param1).Num("param2", o.Param2).Num("value", o.Value);
                if (o.Info != null) j.Str("info", o.Info);
                if (!double.IsNaN(o.Estimate)) j.Raw("estimate", o.Estimate.ToString("0.###", CultureInfo.InvariantCulture));
                sb.Append(j.Close());
            }
            return sb.Append(']').ToString();
        }

        private static string StateJson(StateSnapshot s)
        {
            if (s == null) return "null";
            Json j = new Json();
            j.Num("turn", s.Turn).Str("phase", s.Phase).Num("turnPlayer", s.TurnPlayer).Bool("isFirst", s.IsFirst)
             .Raw("self", PlayerJson(s.Self)).Raw("opponent", PlayerJson(s.Opponent));
            return j.Close();
        }

        private static string PlayerJson(PlayerView p)
        {
            Json j = new Json();
            j.Num("lp", p.LifePoints).Num("deckCount", p.DeckCount)
             .Raw("hand", IntArray(p.Hand)).Raw("monsters", IntArray(p.Monsters)).Raw("spells", IntArray(p.Spells))
             .Raw("graveyard", IntArray(p.Graveyard)).Raw("banished", IntArray(p.Banished)).Raw("extra", IntArray(p.Extra));
            return j.Close();
        }

        private static string IntArray(List<int> xs)
        {
            StringBuilder sb = new StringBuilder("[");
            for (int i = 0; i < xs.Count; ++i)
            {
                if (i > 0) sb.Append(',');
                sb.Append(xs[i].ToString(CultureInfo.InvariantCulture));
            }
            return sb.Append(']').ToString();
        }

        private sealed class Json
        {
            private readonly StringBuilder _sb = new StringBuilder("{");
            private bool _first = true;

            private Json Key(string key)
            {
                if (!_first) _sb.Append(',');
                _first = false;
                _sb.Append('"').Append(key).Append("\":");
                return this;
            }

            public Json Str(string key, string value)
            {
                Key(key);
                if (value == null) { _sb.Append("null"); return this; }
                _sb.Append('"');
                foreach (char c in value)
                {
                    switch (c)
                    {
                        case '"': _sb.Append("\\\""); break;
                        case '\\': _sb.Append("\\\\"); break;
                        case '\n': _sb.Append("\\n"); break;
                        case '\r': _sb.Append("\\r"); break;
                        case '\t': _sb.Append("\\t"); break;
                        default:
                            if (c < 0x20) _sb.Append("\\u").Append(((int)c).ToString("x4"));
                            else _sb.Append(c);
                            break;
                    }
                }
                _sb.Append('"');
                return this;
            }

            public Json Num(string key, long value)
            {
                Key(key);
                _sb.Append(value.ToString(CultureInfo.InvariantCulture));
                return this;
            }

            public Json Bool(string key, bool value)
            {
                Key(key);
                _sb.Append(value ? "true" : "false");
                return this;
            }

            public Json Raw(string key, string json)
            {
                Key(key);
                _sb.Append(json);
                return this;
            }

            public string Close()
            {
                return _sb.ToString() + "}";
            }
        }
    }
}
