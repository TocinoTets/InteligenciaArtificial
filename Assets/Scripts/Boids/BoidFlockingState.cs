namespace IA.Agents
{
    public class BoidFlockingState : BoidState
    {
        public BoidFlockingState(BoidAgent boid) : base(boid) { }

        public override BoidStateId Id => BoidStateId.Flocking;

        public override void Enter()
        {
            boid.SetTargetInterest(null);
        }

        public override bool TryGetTransition(out BoidStateId next)
        {
            if (boid.SeesHunter) return GoTo(BoidStateId.Evade, out next);
            if (boid.SeesInterest) return GoTo(BoidStateId.SeekObject, out next);
            return Stay(out next);
        }

        public override void Execute(float deltaTime)
        {
            boid.StepFlocking(deltaTime);
        }
    }
}