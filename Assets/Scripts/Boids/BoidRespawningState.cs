namespace IA.Agents
{
    public class BoidRespawningState : BoidState
    {
        public BoidRespawningState(BoidAgent boid) : base(boid) { }
        public override BoidStateId Id => BoidStateId.Respawning;

        public override void Enter()
        {
            boid.BeginRespawnWait();
        }

        public override void Tick(float deltaTime)
        {
            boid.TickRespawnWait(deltaTime);
        }
    }
}