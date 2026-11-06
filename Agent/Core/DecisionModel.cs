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
