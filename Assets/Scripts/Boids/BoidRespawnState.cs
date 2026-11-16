namespace IA.Agents
{
    public class BoidRespawnState : BoidState
    {
        public BoidRespawnState(BoidAgent boid) : base(boid) { }

        public override BoidStateId Id => BoidStateId.Respawn;

        public override void Enter()
        {
            boid.BeginRespawn();
        }

        public override bool TryGetTransition(out BoidStateId next)
        {
            if (boid.IsRespawnReady) return GoTo(BoidStateId.Flocking, out next);
            return Stay(out next);
        }

        public override void Execute(float deltaTime)
        {
            boid.TickRespawnTimer(deltaTime);
        }

        public override void Exit()
        {
            boid.RespawnAtRandomPosition();
        }
    }
}