using System.Collections.Generic;

namespace WindBot.Agent.Core
{
    // Neutral decision model. This namespace must not reference WindBot, YGOSharp or packet types,
    // so that the agent core can later move to another runtime (Rust host, Python trainer, ...).

    /// <summary>Which engine prompt produced this request.</summary>
    public enum RequestKind
    {
        RockPaperScissors,
        SelectFirst,
        Idle,
        Battle,
        Chain,
        EffectYn,
        YesNo,
        BattleReplay,
        Option,
        SelectCard,
        SelectUnselect,
        Tribute,
        Sum,
        Counter,
        Place,
        Position,
        SortCard,
        AnnounceCard,
        AnnounceAttribute,
        AnnounceRace,
        AnnounceNumber
    }

    /// <summary>How a valid answer is shaped. Policies work on shapes, not on card-specific logic.</summary>
    public enum RequestShape
    {
        /// <summary>Exactly one option index.</summary>
        One,
        /// <summary>Between Min and Max distinct option indices, or cancel when Cancelable.</summary>
        Many,
        /// <summary>A subset whose Param1/Param2 sum meets Target (exactly when ExactSum).</summary>
        Sum,
        /// <summary>A count per option (each at most Param1) adding up to Quantity.</summary>
        Counter,
        /// <summary>A permutation of all option indices.</summary>
        Order
    }

    public sealed class DecisionOption
    {
        /// <summary>What choosing this option does, e.g. "Summon", "Activate", "Attack", "Pass", "Yes", "Card", "Zone".</summary>
        public string Action;
        public int CardId;
        /// <summary>0 = self, 1 = opponent, -1 = not a card.</summary>
        public int Controller = -1;
        public int Location;
        public int Sequence = -1;
        public int Position;
        /// <summary>Effect description id from the engine, when the option is an effect.</summary>
        public long Description;
        /// <summary>Kind-specific numbers (e.g. sum values, counter capacity).</summary>
        public int Param1;
        public int Param2;
        /// <summary>Kind-specific value (e.g. zone bit, position flag, announced id/number).</summary>
        public long Value;
        /// <summary>Readable knowledge attached from the per-game card DB (effect, usage this turn, linked cards).</summary>
        public string Info;
        /// <summary>Value-model estimate of what this option brings this turn (finite chain sum); NaN when not estimated.</summary>
        public double Estimate = double.NaN;
        /// <summary>The parts the estimate is made of (VALUE_LEARNING_PLAN.md step 1); their Values sum to Estimate. Null when not estimated.</summary>
        public List<ValueTerm> Terms;
    }

    /// <summary>
    /// One part of an option's estimate, so a learner can tell which number fed a decision:
    /// Value = what this part adds (the parts sum to the estimate); Base = the shared value it starts from, Offset = the card
    /// effect's learned offset on it. When Chance is set, Value = (Base + Offset) × Chance × Factor + Add: Chance = γ^depth for
    /// a plan step, the expected number of uses (sum of each use's chance) for an interruption; Factor = shared multipliers
    /// (reactive, both-sides mass, lock on both players); Add = per-use bonuses (solving timing, hand trap) × Count.
    /// So a change d in Offset moves Value by d × Chance × Factor.
    /// Kinds: flow (a plan step), end_hand / end_set / end_zone / end_field / end_payoff (an interruption held at the end of the
    /// turn), follow (a hand card's next-turn use), bonus, draw, battle, efficiency, opp_draw (plan parts); interrupt (a chain
    /// answer now), pass, removal_take, removal_hold, xyz_gain, xyz_hold, bait (reordering of answers offered together);
    /// rule (set by a decision rule, not the value model); residual (a plan replay gap).
    /// </summary>
    public sealed class ValueTerm
    {
        public string Kind;
        public int CardId;
        public int Effect;
        public double Base;
        public double Offset;
        public double Chance = double.NaN;
        public double Factor = 1;
        public double Add;
        public int Count = 1;
        public double Value;
        public string Note;
    }

    /// <summary>
    /// Prediction against what happened, per card effect (VALUE_LEARNING_PLAN.md step 2). For our interruptions:
    /// the end-of-turn terms predicted when our turn ended, and the opponent's turn that followed.
    /// </summary>
    public sealed class OutcomeItem
    {
        public int CardId;
        public int Effect;
        /// <summary>The terms predicted for this card effect (several when copies sit in several places); empty when it was not predicted.</summary>
        public readonly List<ValueTerm> Predicted = new List<ValueTerm>();
        public int Activated;
        public int Resolved;
        public int ActivationNegated;
        public int EffectNegated;
        /// <summary>Still where the prediction counted it when the opponent's turn ended (field, spell zone, hand, as material).</summary>
        public bool Present;
        /// <summary>For each activation, the chain link it answered: (player, card id, effect index); card 0 when it started the chain.</summary>
        public readonly List<int[]> Answered = new List<int[]>();
    }

    public sealed class DecisionRequest
    {
        public RequestKind Kind;
        public RequestShape Shape;
        public readonly List<DecisionOption> Options = new List<DecisionOption>();
        public int Min = 1;
        public int Max = 1;
        public bool Cancelable;
        /// <summary>Selection hint from the engine (HintMsg), 0 when none.</summary>
        public long Hint;
        /// <summary>Effect description for EffectYn / YesNo prompts.</summary>
        public long Description;
        /// <summary>Card the prompt is about (EffectYn, Position), 0 when none.</summary>
        public int SourceCardId;
        public int Target;
        public bool ExactSum;
        public int Quantity;
        /// <summary>The picked cards are given up (cost, discard, material, release): prefer the lowest estimates.</summary>
        public bool PreferLow;
        /// <summary>Never answer at random (e.g. targets among the opponent's cards, responses on the opponent's turn).</summary>
        public bool NoExplore;
        /// <summary>Cancel (when allowed) if no option has a positive estimate, e.g. every attack target loses.</summary>
        public bool CancelIfNotPositive;
    }

    public sealed class Decision
    {
        /// <summary>
        /// One: a single index. Many/Sum: chosen indices. Order: a permutation.
        /// Counter: one count per option, in option order.
        /// </summary>
        public readonly List<int> Indices = new List<int>();
        public bool Canceled;
    }

    /// <summary>Visible duel state at the moment of a decision.</summary>
    public sealed class PlayerView
    {
        public int LifePoints;
        public int DeckCount;
        public readonly List<int> Hand = new List<int>();
        public readonly List<int> Monsters = new List<int>();
        /// <summary>Xyz materials of each monster (ids, 0 = not known), index-aligned with Monsters.</summary>
        public readonly List<List<int>> Materials = new List<List<int>>();
        /// <summary>Face-down monsters / spells and traps (their ids are 0 on the opponent's side).</summary>
        public int SetMonsters, SetSpells;
        public readonly List<int> Spells = new List<int>();
        public readonly List<int> Graveyard = new List<int>();
        public readonly List<int> Banished = new List<int>();
        public readonly List<int> Extra = new List<int>();
    }

    public sealed class StateSnapshot
    {
        public int Turn;
        public string Phase;
        /// <summary>0 = self, 1 = opponent.</summary>
        public int TurnPlayer;
        public bool IsFirst;
        public readonly PlayerView Self = new PlayerView();
        public readonly PlayerView Opponent = new PlayerView();
    }

    /// <summary>The single decision boundary: state + options in, choice out.</summary>
    public interface IDecisionPolicy
    {
        string Name { get; }
        Decision Decide(DecisionRequest request, StateSnapshot state);
    }
}
