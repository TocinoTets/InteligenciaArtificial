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

        public override void Tick(float deltaTime)
        {
            if (boid.WasHarvested) boid.FSM.ChangeState(BoidStateId.Respawning);
        }
    }
}