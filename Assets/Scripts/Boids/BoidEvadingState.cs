namespace IA.Agents
{
    public class BoidEvadeState : BoidState
    {
        public BoidEvadeState(BoidAgent boid) : base(boid) { }

        public override BoidStateId Id => BoidStateId.Evade;

        public override bool TryGetTransition(out BoidStateId next)
        {
            if (boid.SeesHunter) return Stay(out next);
            return GoTo(ResolveCalmState(), out next);
        }

        public override void Execute(float deltaTime)
        {
            boid.StepEvading(deltaTime);
        }
    }
}