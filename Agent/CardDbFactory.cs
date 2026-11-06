using System;
using System.Collections.Generic;
using System.IO;
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

        public static void Dump(string deckName, string outPath)
        {
            Deck deck = Deck.Load(deckName);
            GameCardDb db = Build(deck);
            File.WriteAllText(outPath, db.Describe(null), new UTF8Encoding(false));
            Logger.WriteLine("Card DB: " + db.Cards.Count + " cards, constants " + db.ConstantCount + ", script dirs " + db.ScriptDirCount + " → " + Path.GetFullPath(outPath));
        }
    }
}
