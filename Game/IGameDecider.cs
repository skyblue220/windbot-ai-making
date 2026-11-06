using System.Collections.Generic;
using YGOSharp.OCGWrapper.Enums;

namespace WindBot.Game
{
    /// <summary>
    /// Everything GameBehavior asks of the decision side. GameBehavior keeps packet parsing,
    /// duel-state updates and response encoding; the decider only answers and observes.
    /// Implemented by LegacyDecider (existing GameAI + Executor) and by the new agent bridge.
    /// </summary>
    public interface IGameDecider
    {
        // Lifecycle and notifications
        void OnJoinGame();
        void OnDeckError(string card);
        void OnStart();
        void OnDraw(int player);
        void OnNewTurn();
        void OnNewPhase();
        void OnMove(ClientCard card, int previousControler, int previousLocation, int currentControler, int currentLocation);
        void OnDirectAttack(ClientCard card);
        void OnChaining(ClientCard card, int player);
        void OnChainSolved(int chainIndex);
        void OnChainEnd();
        void OnSpSummoned();
        void OnPlayerHint(int player, int hintType, ulong description);
        void OnHintZone(int player, int zone);
        void OnReceivingAnnouce(int player, long data);
        void CleanSelectMaterials();
        /// <summary>Called after every engine game message has been applied to the duel state.</summary>
        void OnGameMessage(GameMessage message);
        /// <summary>result: 0 = win, 1 = lose, 2 = draw (local perspective).</summary>
        void OnWin(int result);

        // Decisions
        int OnRockPaperScissors();
        bool OnSelectHand();
        MainPhaseAction OnSelectIdleCmd(MainPhase main);
        BattlePhaseAction OnSelectBattleCmd(BattlePhase battle);
        int OnSelectChain(IList<ClientCard> cards, IList<long> descs, bool forced, int timing);
        bool OnSelectEffectYn(ClientCard card, long desc);
        bool OnSelectYesNo(long desc);
        bool OnSelectBattleReplay();
        int OnSelectOption(IList<long> options);
        IList<ClientCard> OnSelectCard(IList<ClientCard> cards, int min, int max, long hint, bool cancelable);
        IList<ClientCard> OnSelectUnselectCard(IList<ClientCard> cards, int min, int max, long hint, bool cancelable);
        IList<ClientCard> OnSelectTribute(IList<ClientCard> cards, int min, int max, long hint, bool cancelable);
        IList<ClientCard> OnSelectSum(IList<ClientCard> cards, int sum, int min, int max, long hint, bool mode);
        IList<int> OnSelectCounter(int type, int quantity, IList<ClientCard> cards, IList<int> counters);
        int OnSelectPlace(long cardId, int player, CardLocation location, int available);
        CardPosition OnSelectPosition(int cardId, IList<CardPosition> positions);
        IList<ClientCard> OnCardSorting(IList<ClientCard> cards);
        int OnAnnounceCard(IList<int> avail);
        int OnAnnounceNumber(IList<int> numbers);
        IList<CardAttribute> OnAnnounceAttrib(int count, IList<CardAttribute> attributes);
        IList<CardRace> OnAnnounceRace(int count, IList<CardRace> races);
    }
}
