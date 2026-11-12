using System;
using System.Collections.Generic;
using System.Linq;
using WindBot.Agent.Knowledge;
using WindBot.Game;
using YGOSharp.OCGWrapper.Enums;

namespace WindBot.Agent
{
    /// <summary>
    /// One side's card locations read from the live duel state, plus everything the other side shows (docs/DECISION_STATE.md).
    /// Player 0 = us; player 1 = the opponent, whose hand shows only revealed cards.
    /// Snapshot() is the one picture every decision reads: both fields with Xyz materials, both GYs and banished zones,
    /// our Extra Deck, the opponent's face-down and hidden counts, LP, phase. What only the agent can compute
    /// (interruptions a card still holds, its battle protection, our summon locks) is filled by Enrich.
    /// </summary>
    public sealed class DuelZoneView : IZoneView
    {
        private readonly Duel _duel;
        private readonly GameCardDb _db;
        private readonly int _player;

        /// <summary>Interruptions an opponent's face-up field card still holds against us (set by the agent).</summary>
        public Func<ClientCard, double> Held;
        /// <summary>Battle protection of an opponent's monster from its own side: (indestructible, replacement cost or -1).</summary>
        public Func<ClientCard, KeyValuePair<bool, double>> Guard;
        /// <summary>The rest the agent knows and the duel does not show (e.g. names we may not Normal Summon this turn).</summary>
        public Action<ZoneSnapshot> Enrich;

        public DuelZoneView(Duel duel, GameCardDb db, int player = 0)
        {
            _duel = duel;
            _db = db;
            _player = player;
        }

        private ClientField Self { get { return _duel.Fields != null ? _duel.Fields[_player] : null; } }
        private ClientField Other { get { return _duel.Fields != null ? _duel.Fields[1 - _player] : null; } }

        public bool NormalSummonAvailable { get { return _player == 0 ? !_db.NormalSummonUsed : !_db.OpponentNormalSummonUsed; } }

        public int FieldMaterials()
        {
            int n = 0;
            if (Self != null && Self.MonsterZone != null)
                foreach (ClientCard c in Self.MonsterZone)
                    if (c != null && c.Overlays != null) n += c.Overlays.Count;
            return n;
        }

        public int Attached(int id)
        {
            int n = 0;
            if (Self != null && Self.MonsterZone != null)
                foreach (ClientCard c in Self.MonsterZone)
                    if (c != null && c.Overlays != null) n += c.Overlays.Count(x => x == id);
            return n;
        }

        public int OpponentFaceUpCount()
        {
            ClientField opp = Other;
            if (opp == null) return -1;
            int up = 0;
            foreach (ClientCard c in opp.MonsterZone) if (c != null && c.IsFaceup()) up++;
            foreach (ClientCard c in opp.SpellZone) if (c != null && c.IsFaceup()) up++;
            return up;
        }

        public ZoneSnapshot Snapshot()
        {
            ZoneSnapshot z = new ZoneSnapshot { NormalSummonAvailable = NormalSummonAvailable };
            if (Self == null || Self.Hand == null) return z;
            // ---- our side
            foreach (ClientCard c in Self.Hand) if (c != null && c.Id != 0) z.Hand.Add(c.Id);
            foreach (ClientCard c in Self.MonsterZone)
                if (c != null && c.Id != 0)
                {
                    z.Monsters.Add(new KeyValuePair<int, int>(c.Id, c.Overlays != null ? c.Overlays.Count : 0));
                    z.MonsterMaterials.Add(c.Overlays != null ? new List<int>(c.Overlays) : new List<int>());
                }
            foreach (ClientCard c in Self.SpellZone)
            {
                if (c == null || c.Id == 0) continue;
                // A face-down spell other than a quick-play does nothing until it is activated.
                bool waits = c.HasType(CardType.Trap) || c.HasType(CardType.QuickPlay);
                if (c.IsFacedown() && !waits) z.SetSpells.Add(c.Id); else z.Spells.Add(c.Id);
            }
            foreach (ClientCard c in Self.Graveyard) if (c != null && c.Id != 0) z.Grave.Add(c.Id);
            foreach (ClientCard c in Self.Banished) if (c != null && c.Id != 0) z.Banished.Add(c.Id);
            if (Self.ExtraDeck != null && Self.ExtraDeck.All(c => c != null && c.Id != 0))
                z.Extra = Self.ExtraDeck.Select(c => c.Id).ToList(); // our own Extra Deck is known by id
            z.DeckCount = Self.Deck != null ? Self.Deck.Count : -1;
            z.OwnLp = Self.LifePoints;
            // ---- the other side
            ClientField opp = Other;
            if (opp != null)
            {
                z.OpponentLp = opp.LifePoints;
                z.OpponentFaceUp = OpponentFaceUpCount();
                z.OpponentHand = opp.Hand != null ? opp.Hand.Count : -1;
                if (opp.Hand != null) foreach (ClientCard c in opp.Hand) if (c != null && c.Id != 0) z.OpponentHandKnown.Add(c.Id);
                z.OpponentDeck = opp.Deck != null ? opp.Deck.Count : -1;
                z.OpponentExtra = opp.ExtraDeck != null ? opp.ExtraDeck.Count : -1;
                foreach (ClientCard c in opp.MonsterZone)
                {
                    if (c == null) continue;
                    bool up = c.IsFaceup() && c.Id != 0;
                    OpponentMonster m = new OpponentMonster { Id = c.Id, Atk = c.Attack, Def = c.Defense, AttackPosition = c.IsAttack(), FaceUp = up };
                    if (c.Overlays != null) m.Materials.AddRange(c.Overlays);
                    if (up && Held != null) m.Held = Held(c);
                    if (up && Guard != null)
                    {
                        KeyValuePair<bool, double> g = Guard(c);
                        m.Indestructible = g.Key;
                        m.ReplaceCost = g.Value;
                    }
                    z.OpponentMonsters.Add(m);
                }
                foreach (ClientCard c in opp.SpellZone)
                {
                    if (c == null) continue;
                    bool up = c.IsFaceup() && c.Id != 0;
                    z.OpponentSpells.Add(new OpponentCard { Id = up ? c.Id : 0, FaceUp = up, Held = up && Held != null ? Held(c) : 0 });
                }
                foreach (ClientCard c in opp.Graveyard)
                {
                    if (c == null || c.Id == 0) continue;
                    z.OpponentGrave.Add(c.Id);
                    if (c.HasType(CardType.Xyz)) z.OpponentGraveXyzRanks += c.Rank;
                }
                foreach (ClientCard c in opp.Banished) if (c != null && c.Id != 0 && c.IsFaceup()) z.OpponentBanished.Add(c.Id);
                z.OpponentDrawWatch.AddRange(_db.DrawWatches(1 - _player));
            }
            // Our Battle Phase is still ahead: not the duel's first turn, our turn, Main Phase 1.
            z.CanAttack = _duel.Turn > 1 && _duel.Player == _player && _duel.Phase == DuelPhase.Main1;
            z.Full = true;
            if (Enrich != null) Enrich(z);
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
