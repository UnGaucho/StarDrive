using Microsoft.Xna.Framework;


namespace Ship_Game.AI.CombatTactics
{
    internal sealed class HoldPosition : ShipAIPlan
    {
        GameplayObject Target;
        public HoldPosition(ShipAI ai) : base(ai)
        {
        }
        public override void Execute(float elapsedTime, ShipAI.ShipGoal g)
        {
            Target = AI.Target;
            AI.ReverseThrustUntilStopped(elapsedTime);
            Vector2 interceptPoint = Owner.PredictImpact(Target);
            AI.RotateTowardsPosition(interceptPoint, elapsedTime, 0.2f);

        }
    }
}
