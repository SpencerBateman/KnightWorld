using System.Collections.Generic;

namespace Knightworld.Core
{
    public sealed class MoveResult
    {
        public bool Success { get; set; }
        public string FailReason { get; set; }
        public List<GridPos> PathTaken { get; } = new List<GridPos>();
        public List<AttackResult> OpportunityAttacks { get; } = new List<AttackResult>();
        public bool MoverDied { get; set; }
    }

    public sealed class EndTurnResult
    {
        public int PreviousUnitId { get; set; }
        public int NextUnitId { get; set; }
        public int Round { get; set; }
    }
}
