using System;
using System.Collections.Generic;
using WindBot.Agent.Knowledge;
using WindBot.Game;

namespace WindBot.Agent
{
    /// <summary>Own card locations read from the live duel state (deck contents stay hidden; the value model derives them).</summary>
    public sealed class DuelZoneView : IZoneView
    {
        private readonly Duel _duel;
        private readonly GameCardDb _db;

        public DuelZoneView(Duel duel, GameCardDb db)
        {
            _duel = duel;
            _db = db;
        }

        private ClientField Self { get { return _duel.Fields != null ? _duel.Fields[0] : null; } }

        public bool NormalSummonAvailable { get { return !_db.NormalSummonUsed; } }

        public int FieldMaterials()
        {
            int n = 0;
            if (Self != null && Self.MonsterZone != null)
                foreach (ClientCard c in Self.MonsterZone)
                    if (c != null && c.Overlays != null) n += c.Overlays.Count;
            return n;
        }

        public ZoneSnapshot Snapshot()
        {
            ZoneSnapshot z = new ZoneSnapshot { NormalSummonAvailable = NormalSummonAvailable };
            if (Self == null || Self.Hand == null) return z;
            foreach (ClientCard c in Self.Hand) if (c != null && c.Id != 0) z.Hand.Add(c.Id);
            foreach (ClientCard c in Self.MonsterZone)
                if (c != null && c.Id != 0) z.Monsters.Add(new KeyValuePair<int, int>(c.Id, c.Overlays != null ? c.Overlays.Count : 0));
            foreach (ClientCard c in Self.SpellZone) if (c != null && c.Id != 0) z.Spells.Add(c.Id);
            foreach (ClientCard c in Self.Graveyard) if (c != null && c.Id != 0) z.Grave.Add(c.Id);
            foreach (ClientCard c in Self.Banished) if (c != null && c.Id != 0) z.Banished.Add(c.Id);
            return z;
        }

        public int Hand(int id) { return Self == null ? 0 : Count(Self.Hand, id); }
        public int Field(int id) { return Self == null ? 0 : Count(Self.MonsterZone, id) + Count(Self.SpellZone, id); }
        public int Grave(int id) { return Self == null ? 0 : Count(Self.Graveyard, id); }
        public int Banished(int id) { return Self == null ? 0 : Count(Self.Banished, id); }

        public bool AnyOwn(string zones, Func<int, bool> match)
        {
            if (Self == null) return false;
            foreach (string zone in StaticZoneView.ZoneNames(zones))
            {
                IEnumerable<ClientCard> cards = null;
                switch (zone)
                {
                    case "HAND": cards = Self.Hand; break;
                    case "MZONE": cards = Self.MonsterZone; break;
                    case "SZONE": cards = Self.SpellZone; break;
                    case "GRAVE": cards = Self.Graveyard; break;
                    case "REMOVED": cards = Self.Banished; break;
                }
                if (cards == null) continue;
                foreach (ClientCard c in cards)
                    if (c != null && c.Id != 0 && match(c.Id)) return true;
            }
            return false;
        }

        private static int Count(IEnumerable<ClientCard> cards, int id)
        {
            int n = 0;
            if (cards != null) foreach (ClientCard c in cards) if (c != null && c.Id == id) n++;
            return n;
        }
    }
}
