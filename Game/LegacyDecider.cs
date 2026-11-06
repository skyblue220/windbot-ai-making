using System.Collections.Generic;
using YGOSharp.OCGWrapper.Enums;

namespace WindBot.Game
{
    /// <summary>Pure delegation to the existing GameAI/Executor path, unchanged behavior for all legacy decks.</summary>
    public sealed class LegacyDecider : IGameDecider
    {
        private readonly GameAI _ai;

        public LegacyDecider(GameAI ai)
        {
            _ai = ai;
        }

        public void OnDeckLoaded(Deck deck) { }
        public void OnChainNegated(int chainIndex) { }
        public void OnChainDisabled(int chainIndex) { }
        public void OnJoinGame() { _ai.OnJoinGame(); }
        public void OnDeckError(string card) { _ai.OnDeckError(card); }
        public void OnStart() { _ai.OnStart(); }
        public void OnDraw(int player) { _ai.OnDraw(player); }
        public void OnNewTurn() { _ai.OnNewTurn(); }
        public void OnNewPhase() { _ai.OnNewPhase(); }
        public void OnMove(ClientCard card, int previousControler, int previousLocation, int currentControler, int currentLocation)
        { _ai.OnMove(card, previousControler, previousLocation, currentControler, currentLocation); }
        public void OnDirectAttack(ClientCard card) { _ai.OnDirectAttack(card); }
        public void OnChaining(ClientCard card, int player) { _ai.OnChaining(card, player); }
        public void OnChainSolved(int chainIndex) { _ai.OnChainSolved(chainIndex); }
        public void OnChainEnd() { _ai.OnChainEnd(); }
        public void OnSpSummoned() { _ai.OnSpSummoned(); }
        public void OnPlayerHint(int player, int hintType, ulong description) { _ai.OnPlayerHint(player, hintType, description); }
        public void OnHintZone(int player, int zone) { _ai.OnHintZone(player, zone); }
        public void OnReceivingAnnouce(int player, long data) { _ai.OnReceivingAnnouce(player, data); }
        public void CleanSelectMaterials() { _ai.CleanSelectMaterials(); }
        public void OnGameMessage(GameMessage message) { }
        public void OnWin(int result) { }

        public int OnRockPaperScissors() { return _ai.OnRockPaperScissors(); }
        public bool OnSelectHand() { return _ai.OnSelectHand(); }
        public MainPhaseAction OnSelectIdleCmd(MainPhase main) { return _ai.OnSelectIdleCmd(main); }
        public BattlePhaseAction OnSelectBattleCmd(BattlePhase battle) { return _ai.OnSelectBattleCmd(battle); }
        public int OnSelectChain(IList<ClientCard> cards, IList<long> descs, bool forced, int timing) { return _ai.OnSelectChain(cards, descs, forced, timing); }
        public bool OnSelectEffectYn(ClientCard card, long desc) { return _ai.OnSelectEffectYn(card, desc); }
        public bool OnSelectYesNo(long desc) { return _ai.OnSelectYesNo(desc); }
        public bool OnSelectBattleReplay() { return _ai.OnSelectBattleReplay(); }
        public int OnSelectOption(IList<long> options) { return _ai.OnSelectOption(options); }
        public IList<ClientCard> OnSelectCard(IList<ClientCard> cards, int min, int max, long hint, bool cancelable) { return _ai.OnSelectCard(cards, min, max, hint, cancelable); }
        public IList<ClientCard> OnSelectUnselectCard(IList<ClientCard> cards, int min, int max, long hint, bool cancelable) { return _ai.OnSelectCard(cards, min, max, hint, cancelable); }
        public IList<ClientCard> OnSelectTribute(IList<ClientCard> cards, int min, int max, long hint, bool cancelable) { return _ai.OnSelectTribute(cards, min, max, hint, cancelable); }
        public IList<ClientCard> OnSelectSum(IList<ClientCard> cards, int sum, int min, int max, long hint, bool mode) { return _ai.OnSelectSum(cards, sum, min, max, hint, mode); }
        public IList<int> OnSelectCounter(int type, int quantity, IList<ClientCard> cards, IList<int> counters) { return _ai.OnSelectCounter(type, quantity, cards, counters); }
        public int OnSelectPlace(long cardId, int player, CardLocation location, int available) { return _ai.OnSelectPlace(cardId, player, location, available); }
        public CardPosition OnSelectPosition(int cardId, IList<CardPosition> positions) { return _ai.OnSelectPosition(cardId, positions); }
        public IList<ClientCard> OnCardSorting(IList<ClientCard> cards) { return _ai.OnCardSorting(cards); }
        public int OnAnnounceCard(IList<int> avail) { return _ai.OnAnnounceCard(avail); }
        public int OnAnnounceNumber(IList<int> numbers) { return _ai.OnAnnounceNumber(numbers); }
        public IList<CardAttribute> OnAnnounceAttrib(int count, IList<CardAttribute> attributes) { return _ai.OnAnnounceAttrib(count, attributes); }
        public IList<CardRace> OnAnnounceRace(int count, IList<CardRace> races) { return _ai.OnAnnounceRace(count, races); }
    }
}
