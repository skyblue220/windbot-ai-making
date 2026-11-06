using System;
using System.Collections.Generic;
using WindBot.Agent.Core;
using WindBot.Game;
using YGOSharp.OCGWrapper.Enums;

namespace WindBot.Agent
{
    /// <summary>
    /// Adapter between GameBehavior (WindBot types) and the neutral agent core.
    /// Converts every engine prompt into a DecisionRequest, asks the policy, and maps the
    /// chosen indices back to the WindBot return types that GameBehavior encodes.
    /// Contains no card-specific logic and never touches GameAI or any Executor.
    /// </summary>
    public sealed class AgentBridge : IGameDecider
    {
        public const string DeckPrefix = "Agent:";

        public static bool IsAgentDeck(string deck)
        {
            return deck != null && deck.StartsWith(DeckPrefix, StringComparison.OrdinalIgnoreCase);
        }

        public static string DeckNameOf(string deck)
        {
            string name = deck.Substring(DeckPrefix.Length).Trim();
            return name.Length == 0 ? "AI_Ryzeal" : name;
        }

        private readonly Duel _duel;
        private readonly string _deck;
        private readonly string _buildId;
        private IDecisionPolicy _policy;
        private AgentLog _log;
        private bool _duelFinished;

        public AgentBridge(Duel duel, string deck)
        {
            _duel = duel;
            _deck = deck;
            _buildId = typeof(AgentBridge).Module.ModuleVersionId.ToString("N");
            BeginDuelLog();
        }

        private void BeginDuelLog()
        {
            int seed = Environment.TickCount ^ Guid.NewGuid().GetHashCode();
            string duelId = Guid.NewGuid().ToString("N");
            _policy = new RandomPolicy(seed);
            _log = new AgentLog(System.IO.Path.Combine("logs", "agent"), duelId);
            _log.Header(duelId, _buildId, _policy.Name, seed, _deck);
            _duelFinished = false;
            Logger.WriteLine("Agent log: " + _log.Path);
        }

        // ---------------------------------------------------------------- core call

        private Decision Ask(DecisionRequest request)
        {
            StateSnapshot state = Snapshot();
            Decision decision;
            try
            {
                decision = _policy.Decide(request, state);
            }
            catch (Exception ex)
            {
                _log.Error("policy:" + request.Kind, ex.ToString());
                throw;
            }
            _log.Decision(request, state, decision);
            return decision;
        }

        private int AskOne(DecisionRequest request)
        {
            request.Shape = RequestShape.One;
            Decision d = Ask(request);
            return d.Canceled || d.Indices.Count == 0 ? -1 : d.Indices[0];
        }

        private static DecisionRequest Request(RequestKind kind)
        {
            DecisionRequest r = new DecisionRequest();
            r.Kind = kind;
            return r;
        }

        private static DecisionOption Option(string action)
        {
            DecisionOption o = new DecisionOption();
            o.Action = action;
            return o;
        }

        private static DecisionOption CardOption(string action, ClientCard card)
        {
            DecisionOption o = Option(action);
            if (card != null)
            {
                o.CardId = card.Id;
                o.Controller = card.Controller;
                o.Location = (int)card.Location;
                o.Sequence = card.Sequence;
                o.Position = card.Position;
            }
            return o;
        }

        private static void AddYesNo(DecisionRequest r)
        {
            r.Options.Add(Option("Yes"));
            r.Options.Add(Option("No"));
        }

        // ---------------------------------------------------------------- state

        private StateSnapshot Snapshot()
        {
            StateSnapshot s = new StateSnapshot();
            s.Turn = _duel.Turn;
            s.Phase = _duel.Phase.ToString();
            s.TurnPlayer = _duel.Player;
            s.IsFirst = _duel.IsFirst;
            // Before the duel starts (rock-paper-scissors, going first) the fields may not exist yet.
            if (_duel.Fields != null && _duel.Fields.Length >= 2)
            {
                FillPlayer(s.Self, _duel.Fields[0]);
                FillPlayer(s.Opponent, _duel.Fields[1]);
            }
            return s;
        }

        private static void FillPlayer(PlayerView view, ClientField field)
        {
            if (field == null) return;
            view.LifePoints = field.LifePoints;
            view.DeckCount = field.Deck != null ? field.Deck.Count : 0;
            AddIds(view.Hand, field.Hand);
            AddIds(view.Monsters, field.MonsterZone);
            AddIds(view.Spells, field.SpellZone);
            AddIds(view.Graveyard, field.Graveyard);
            AddIds(view.Banished, field.Banished);
            AddIds(view.Extra, field.ExtraDeck);
        }

        private static void AddIds(List<int> target, IEnumerable<ClientCard> cards)
        {
            if (cards == null) return;
            foreach (ClientCard c in cards)
                if (c != null) target.Add(c.Id);
        }

        // ---------------------------------------------------------------- notifications

        public void OnJoinGame() { }
        public void OnDeckError(string card) { _log.Error("deck", card); }

        public void OnStart()
        {
            if (_duelFinished) BeginDuelLog();
        }

        public void OnDraw(int player) { }
        public void OnNewTurn() { }
        public void OnNewPhase() { }
        public void OnMove(ClientCard card, int previousControler, int previousLocation, int currentControler, int currentLocation) { }
        public void OnDirectAttack(ClientCard card) { }
        public void OnChaining(ClientCard card, int player) { }
        public void OnChainSolved(int chainIndex) { }
        public void OnChainEnd() { }
        public void OnSpSummoned() { }
        public void OnPlayerHint(int player, int hintType, ulong description) { }
        public void OnHintZone(int player, int zone) { }
        public void OnReceivingAnnouce(int player, long data) { }
        public void CleanSelectMaterials() { }

        public void OnGameMessage(GameMessage message)
        {
            _log.Message(message.ToString(), _duel.Turn, _duel.Phase.ToString());
        }

        public void OnWin(int result)
        {
            _log.Result(result == 2 ? "Draw" : result == 0 ? "Win" : "Lose", _duel.Turn);
            _duelFinished = true;
        }

        // ---------------------------------------------------------------- decisions

        public int OnRockPaperScissors()
        {
            DecisionRequest r = Request(RequestKind.RockPaperScissors);
            for (int v = 1; v <= 3; ++v)
            {
                DecisionOption o = Option("Hand");
                o.Value = v;
                r.Options.Add(o);
            }
            int i = AskOne(r);
            return i < 0 ? 1 : (int)r.Options[i].Value;
        }

        public bool OnSelectHand()
        {
            DecisionRequest r = Request(RequestKind.SelectFirst);
            r.Options.Add(Option("GoFirst"));
            r.Options.Add(Option("GoSecond"));
            return AskOne(r) != 1;
        }

        public MainPhaseAction OnSelectIdleCmd(MainPhase main)
        {
            DecisionRequest r = Request(RequestKind.Idle);
            List<MainPhaseAction> actions = new List<MainPhaseAction>();

            AddIdle(r, actions, main.SummonableCards, "Summon", MainPhaseAction.MainAction.Summon, 0);
            AddIdle(r, actions, main.SpecialSummonableCards, "SpSummon", MainPhaseAction.MainAction.SpSummon, 1);
            AddIdle(r, actions, main.ReposableCards, "Repos", MainPhaseAction.MainAction.Repos, 2);
            AddIdle(r, actions, main.MonsterSetableCards, "SetMonster", MainPhaseAction.MainAction.SetMonster, 3);
            AddIdle(r, actions, main.SpellSetableCards, "SetSpell", MainPhaseAction.MainAction.SetSpell, 4);
            for (int i = 0; i < main.ActivableCards.Count; ++i)
            {
                ClientCard card = main.ActivableCards[i];
                long desc = main.ActivableDescs[i];
                int index;
                if (!card.ActionActivateIndex.TryGetValue(desc, out index)) index = card.ActionIndex[5];
                DecisionOption o = CardOption("Activate", card);
                o.Description = desc;
                r.Options.Add(o);
                actions.Add(new MainPhaseAction(MainPhaseAction.MainAction.Activate, index));
            }
            if (main.CanBattlePhase)
            {
                r.Options.Add(Option("ToBattlePhase"));
                actions.Add(new MainPhaseAction(MainPhaseAction.MainAction.ToBattlePhase));
            }
            if (main.CanEndPhase)
            {
                r.Options.Add(Option("ToEndPhase"));
                actions.Add(new MainPhaseAction(MainPhaseAction.MainAction.ToEndPhase));
            }

            int chosen = AskOne(r);
            return chosen < 0 ? new MainPhaseAction(MainPhaseAction.MainAction.ToEndPhase) : actions[chosen];
        }

        private static void AddIdle(DecisionRequest r, List<MainPhaseAction> actions, IList<ClientCard> cards,
            string label, MainPhaseAction.MainAction action, int slot)
        {
            foreach (ClientCard card in cards)
            {
                r.Options.Add(CardOption(label, card));
                actions.Add(new MainPhaseAction(action, card.ActionIndex[slot]));
            }
        }

        public BattlePhaseAction OnSelectBattleCmd(BattlePhase battle)
        {
            DecisionRequest r = Request(RequestKind.Battle);
            List<BattlePhaseAction> actions = new List<BattlePhaseAction>();
            for (int i = 0; i < battle.ActivableCards.Count; ++i)
            {
                ClientCard card = battle.ActivableCards[i];
                DecisionOption o = CardOption("Activate", card);
                o.Description = battle.ActivableDescs[i];
                r.Options.Add(o);
                actions.Add(new BattlePhaseAction(BattlePhaseAction.BattleAction.Activate, card.ActionIndex));
            }
            foreach (ClientCard card in battle.AttackableCards)
            {
                DecisionOption o = CardOption("Attack", card);
                o.Value = card.CanDirectAttack ? 1 : 0;
                r.Options.Add(o);
                actions.Add(new BattlePhaseAction(BattlePhaseAction.BattleAction.Attack, card.ActionIndex));
            }
            if (battle.CanMainPhaseTwo)
            {
                r.Options.Add(Option("ToMainPhaseTwo"));
                actions.Add(new BattlePhaseAction(BattlePhaseAction.BattleAction.ToMainPhaseTwo));
            }
            if (battle.CanEndPhase)
            {
                r.Options.Add(Option("ToEndPhase"));
                actions.Add(new BattlePhaseAction(BattlePhaseAction.BattleAction.ToEndPhase));
            }

            int chosen = AskOne(r);
            return chosen < 0 ? new BattlePhaseAction(BattlePhaseAction.BattleAction.ToEndPhase) : actions[chosen];
        }

        public int OnSelectChain(IList<ClientCard> cards, IList<long> descs, bool forced, int timing)
        {
            DecisionRequest r = Request(RequestKind.Chain);
            r.Hint = timing;
            for (int i = 0; i < cards.Count; ++i)
            {
                DecisionOption o = CardOption("Activate", cards[i]);
                o.Description = descs[i];
                r.Options.Add(o);
            }
            if (!forced) r.Options.Add(Option("Pass"));
            int chosen = AskOne(r);
            if (chosen < 0 || chosen >= cards.Count) return forced ? 0 : -1;
            return chosen;
        }

        public bool OnSelectEffectYn(ClientCard card, long desc)
        {
            DecisionRequest r = Request(RequestKind.EffectYn);
            r.Description = desc;
            r.SourceCardId = card != null ? card.Id : 0;
            AddYesNo(r);
            return AskOne(r) == 0;
        }

        public bool OnSelectYesNo(long desc)
        {
            DecisionRequest r = Request(RequestKind.YesNo);
            r.Description = desc;
            AddYesNo(r);
            return AskOne(r) == 0;
        }

        public bool OnSelectBattleReplay()
        {
            DecisionRequest r = Request(RequestKind.BattleReplay);
            AddYesNo(r);
            return AskOne(r) == 0;
        }

        public int OnSelectOption(IList<long> options)
        {
            DecisionRequest r = Request(RequestKind.Option);
            foreach (long desc in options)
            {
                DecisionOption o = Option("Option");
                o.Description = desc;
                r.Options.Add(o);
            }
            int chosen = AskOne(r);
            return chosen < 0 ? 0 : chosen;
        }

        public IList<ClientCard> OnSelectCard(IList<ClientCard> cards, int min, int max, long hint, bool cancelable)
        {
            return SelectMany(RequestKind.SelectCard, cards, min, max, hint, cancelable);
        }

        public IList<ClientCard> OnSelectUnselectCard(IList<ClientCard> cards, int min, int max, long hint, bool cancelable)
        {
            return SelectMany(RequestKind.SelectUnselect, cards, min, max, hint, cancelable);
        }

        public IList<ClientCard> OnSelectTribute(IList<ClientCard> cards, int min, int max, long hint, bool cancelable)
        {
            return SelectMany(RequestKind.Tribute, cards, min, max, hint, cancelable);
        }

        private IList<ClientCard> SelectMany(RequestKind kind, IList<ClientCard> cards, int min, int max, long hint, bool cancelable)
        {
            DecisionRequest r = Request(kind);
            r.Shape = RequestShape.Many;
            r.Min = min;
            r.Max = max;
            r.Hint = hint;
            r.Cancelable = cancelable;
            foreach (ClientCard card in cards) r.Options.Add(CardOption("Card", card));
            Decision d = Ask(r);
            List<ClientCard> selected = new List<ClientCard>();
            if (!d.Canceled)
                foreach (int i in d.Indices) selected.Add(cards[i]);
            return selected;
        }

        public IList<ClientCard> OnSelectSum(IList<ClientCard> cards, int sum, int min, int max, long hint, bool mode)
        {
            DecisionRequest r = Request(RequestKind.Sum);
            r.Shape = RequestShape.Sum;
            r.Min = min;
            r.Max = max;
            r.Hint = hint;
            r.Target = sum;
            r.ExactSum = mode;
            foreach (ClientCard card in cards)
            {
                DecisionOption o = CardOption("Card", card);
                o.Param1 = card.OpParam1;
                o.Param2 = card.OpParam2;
                r.Options.Add(o);
            }
            Decision d = Ask(r);
            List<ClientCard> selected = new List<ClientCard>();
            if (d.Canceled) _log.Error("sum", "no subset meets target " + sum);
            else foreach (int i in d.Indices) selected.Add(cards[i]);
            return selected;
        }

        public IList<int> OnSelectCounter(int type, int quantity, IList<ClientCard> cards, IList<int> counters)
        {
            DecisionRequest r = Request(RequestKind.Counter);
            r.Shape = RequestShape.Counter;
            r.Quantity = quantity;
            r.Hint = type;
            for (int i = 0; i < cards.Count; ++i)
            {
                DecisionOption o = CardOption("Card", cards[i]);
                o.Param1 = counters[i];
                r.Options.Add(o);
            }
            Decision d = Ask(r);
            List<int> used = new List<int>();
            for (int i = 0; i < cards.Count; ++i) used.Add(i < d.Indices.Count ? d.Indices[i] : 0);
            return used;
        }

        public int OnSelectPlace(long cardId, int player, CardLocation location, int available)
        {
            DecisionRequest r = Request(RequestKind.Place);
            r.Hint = cardId;
            for (int bit = 0; bit < 31; ++bit)
            {
                if ((available & (1 << bit)) == 0) continue;
                DecisionOption o = Option("Zone");
                o.Controller = player;
                o.Location = (int)location;
                o.Sequence = bit;
                o.Value = 1 << bit;
                r.Options.Add(o);
            }
            int chosen = AskOne(r);
            return chosen < 0 ? 0 : (int)r.Options[chosen].Value;
        }

        public CardPosition OnSelectPosition(int cardId, IList<CardPosition> positions)
        {
            DecisionRequest r = Request(RequestKind.Position);
            r.SourceCardId = cardId;
            foreach (CardPosition p in positions)
            {
                DecisionOption o = Option("Position");
                o.Value = (int)p;
                r.Options.Add(o);
            }
            int chosen = AskOne(r);
            return chosen < 0 ? positions[0] : positions[chosen];
        }

        public IList<ClientCard> OnCardSorting(IList<ClientCard> cards)
        {
            DecisionRequest r = Request(RequestKind.SortCard);
            r.Shape = RequestShape.Order;
            r.Min = r.Max = cards.Count;
            foreach (ClientCard card in cards) r.Options.Add(CardOption("Card", card));
            Decision d = Ask(r);
            List<ClientCard> ordered = new List<ClientCard>();
            foreach (int i in d.Indices) ordered.Add(cards[i]);
            return ordered.Count == cards.Count ? ordered : new List<ClientCard>(cards);
        }

        public int OnAnnounceCard(IList<int> avail)
        {
            DecisionRequest r = Request(RequestKind.AnnounceCard);
            foreach (int id in avail)
            {
                DecisionOption o = Option("Announce");
                o.CardId = id;
                o.Value = id;
                r.Options.Add(o);
            }
            int chosen = AskOne(r);
            return avail[chosen < 0 ? 0 : chosen];
        }

        public int OnAnnounceNumber(IList<int> numbers)
        {
            DecisionRequest r = Request(RequestKind.AnnounceNumber);
            foreach (int n in numbers)
            {
                DecisionOption o = Option("Number");
                o.Value = n;
                r.Options.Add(o);
            }
            int chosen = AskOne(r);
            return chosen < 0 ? 0 : chosen; // GameBehavior expects an index
        }

        public IList<CardAttribute> OnAnnounceAttrib(int count, IList<CardAttribute> attributes)
        {
            DecisionRequest r = Request(RequestKind.AnnounceAttribute);
            r.Shape = RequestShape.Many;
            r.Min = r.Max = count;
            foreach (CardAttribute a in attributes)
            {
                DecisionOption o = Option("Attribute");
                o.Value = (int)a;
                r.Options.Add(o);
            }
            Decision d = Ask(r);
            List<CardAttribute> chosen = new List<CardAttribute>();
            foreach (int i in d.Indices) chosen.Add(attributes[i]);
            foreach (CardAttribute a in attributes) if (chosen.Count < count && !chosen.Contains(a)) chosen.Add(a);
            return chosen;
        }

        public IList<CardRace> OnAnnounceRace(int count, IList<CardRace> races)
        {
            DecisionRequest r = Request(RequestKind.AnnounceRace);
            r.Shape = RequestShape.Many;
            r.Min = r.Max = count;
            foreach (CardRace race in races)
            {
                DecisionOption o = Option("Race");
                o.Value = (long)race;
                r.Options.Add(o);
            }
            Decision d = Ask(r);
            List<CardRace> chosen = new List<CardRace>();
            foreach (int i in d.Indices) chosen.Add(races[i]);
            foreach (CardRace race in races) if (chosen.Count < count && !chosen.Contains(race)) chosen.Add(race);
            return chosen;
        }
    }
}
