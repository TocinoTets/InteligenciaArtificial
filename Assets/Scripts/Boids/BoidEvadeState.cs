namespace IA.Agents
{
    public class BoidEvadingState : BoidState
    {
        public BoidEvadingState(BoidAgent boid) : base(boid) { }
        public override BoidStateId Id => BoidStateId.Evading;

        public override void Tick(float deltaTime)
        {
            if (TryDie()) return;

            if (boid.DetectedHunter != null)
            {
                boid.RefreshThreatMemory(boid.DetectedHunter.transform.position);
            }
            else
            {
                boid.TickThreatMemory(deltaTime);
                if (!boid.HasThreatMemory)
                {
                    boid.FSM.ChangeState(BoidStateId.Flocking);
                    return;
                }
            }

            boid.StepEvading(deltaTime);
        }
    }
}