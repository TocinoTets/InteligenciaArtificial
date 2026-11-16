namespace IA.Agents
{
    public class BoidConsumeState : BoidState
    {
        public BoidConsumeState(BoidAgent boid) : base(boid) { }

        public override BoidStateId Id => BoidStateId.Consume;

        public override void Enter()
        {
            boid.ResetInteractionTimer();
        }

        public override bool TryGetTransition(out BoidStateId next)
        {
            if (boid.SeesHunter) return GoTo(BoidStateId.Evade, out next);
            if (!boid.HasValidTarget) return GoTo(BoidStateId.Flocking, out next);
            if (!boid.IsTargetWithinConsumeRange) return GoTo(BoidStateId.SeekObject, out next);
            return Stay(out next);
        }

        public override void Execute(float deltaTime)
        {
            boid.StepTowardsInterest(deltaTime);
            boid.TickInteraction(deltaTime);
        }
    }
}