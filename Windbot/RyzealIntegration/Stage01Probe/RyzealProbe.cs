using System;
using System.IO;
using WindBot.Game;
using WindBot.Game.AI;

namespace RyzealResearch
{
    [Deck("RyzealResearchProbe", "AI_Ryzeal")]
    public class RyzealProbe : DefaultExecutor
    {
        public RyzealProbe(GameAI ai, Duel duel) : base(ai, duel)
        {
            // Intentionally no gameplay policy: only prove assembly load and instantiation.
            try
            {
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                Directory.CreateDirectory(dir);
                File.AppendAllText(Path.Combine(dir, "ryzeal-plugin-probe.jsonl"),
                    "{\"eventType\":\"executorConstructed\",\"executor\":\"RyzealResearchProbe\",\"timestampUtc\":\"" +
                    DateTime.UtcNow.ToString("o") + "\"}" + Environment.NewLine);
            }
            catch (IOException ex) { Console.Error.WriteLine(ex.Message); }
            catch (UnauthorizedAccessException ex) { Console.Error.WriteLine(ex.Message); }
        }
    }
}
