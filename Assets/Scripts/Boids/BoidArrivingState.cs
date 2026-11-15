namespace IA.Agents
{
    public class BoidArrivingState : BoidState
    {
        public BoidArrivingState(BoidAgent boid) : base(boid) { }
        public override BoidStateId Id => BoidStateId.Arriving;

        public override void Tick(float deltaTime)
        {
            if (TryEvade()) return;

            InterestObject target = boid.TargetInterest;
            if (target == null || !target.IsAvailable)
            {
                boid.SetTargetInterest(null);
                boid.FSM.ChangeState(BoidStateId.Flocking);
                return;
            }

            float sqrDistance = AgentMath.FlatSqrDistance(boid.Position, target.transform.position);
            if (sqrDistance <= boid.InteractionDistance * boid.InteractionDistance)
            {
                boid.FSM.ChangeState(BoidStateId.Interacting);
                return;
            }

            boid.StepTowardsInterest(deltaTime);
        }
    }
}