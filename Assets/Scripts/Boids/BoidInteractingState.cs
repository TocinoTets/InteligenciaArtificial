namespace IA.Agents
{
    public class BoidInteractingState : BoidState
    {
        public BoidInteractingState(BoidAgent boid) : base(boid) { }
        public override BoidStateId Id => BoidStateId.Interacting;

        public override void Enter()
        {
            boid.ResetInteractionTimer();
        }

        public override void Tick(float deltaTime)
        {
            if (TryDie() || TryEvade()) return;

            InterestObject target = boid.TargetInterest;
            if (target == null || !target.IsAvailable)
            {
                boid.SetTargetInterest(null);
                boid.FSM.ChangeState(BoidStateId.Flocking);
                return;
            }

            float sqrDistance = AgentMath.FlatSqrDistance(boid.Position, target.transform.position);
            if (sqrDistance > boid.InteractionDistance * boid.InteractionDistance)
            {
                boid.FSM.ChangeState(BoidStateId.Arriving);
                return;
            }

            boid.StepTowardsInterest(deltaTime);
            boid.TickInteraction(deltaTime);
        }
    }
}