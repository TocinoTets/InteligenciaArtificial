namespace IA.Agents
{
    public class BoidSeekObjectState : BoidState
    {
        public BoidSeekObjectState(BoidAgent boid) : base(boid) { }

        public override BoidStateId Id => BoidStateId.SeekObject;

        public override void Enter()
        {
            if (!boid.HasValidTarget) boid.SetTargetInterest(boid.ClosestVisibleInterest);
        }

        public override bool TryGetTransition(out BoidStateId next)
        {
            if (boid.SeesHunter) return GoTo(BoidStateId.Evade, out next);
            if (boid.IsTargetInReach) return GoTo(BoidStateId.Consume, out next);
            if (!boid.HasValidTarget) return GoTo(BoidStateId.Flocking, out next);
            return Stay(out next);
        }

        public override void Execute(float deltaTime)
        {
            boid.StepTowardsInterest(deltaTime);
        }
    }
}