namespace IA.Agents
{
    public class BoidDeadState : BoidState
    {
        public BoidDeadState(BoidAgent boid) : base(boid) { }

        public override BoidStateId Id => BoidStateId.Dead;

        public override void Enter()
        {
            boid.StopForDeath();
        }

        public override bool TryGetTransition(out BoidStateId next)
        {
            if (boid.WasHarvested) return GoTo(BoidStateId.Respawn, out next);
            return Stay(out next);
        }

        public override void Execute(float deltaTime) { }
    }
}