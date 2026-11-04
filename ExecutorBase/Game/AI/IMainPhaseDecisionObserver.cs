namespace WindBot.Game.AI
{
    // Optional observer; other decks need not implement it.
    public interface IMainPhaseDecisionObserver
    {
        void ObserveDuelStart();
        void ObserveLegacyMainPhaseAction(string kind, int cardId, long description);
        // kind: "EffectYn" (SelectEffectYn) or "YesNo" (SelectYesNo). cardId is 0 for YesNo.
        void ObservePrompt(string kind, int cardId, long description, bool answer);
    }
}
