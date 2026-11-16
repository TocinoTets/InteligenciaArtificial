namespace IA.Agents
{
    public class BoidFlockingState : BoidState
    {
        public BoidFlockingState(BoidAgent boid) : base(boid) { }
        public override BoidStateId Id => BoidStateId.Flocking;

        public override void Tick(float deltaTime)
        {
            if (TryDie() || TryEvade()) return;

            InterestObject target = boid.TargetInterest;
            bool targetValid = target != null && target.IsAvailable &&
                AgentMath.FlatSqrDistance(boid.Position, target.transform.position) <=
                boid.InterestSightRadius * boid.InterestSightRadius * 1.7f;

            if (!targetValid) target = boid.SelectInterest();

            if (target != null)
            {
                boid.SetTargetInterest(target);
                float sqrDistance = AgentMath.FlatSqrDistance(boid.Position, target.transform.position);
                boid.FSM.ChangeState(sqrDistance <= boid.InteractionDistance * boid.InteractionDistance
                    ? BoidStateId.Interacting
                    : BoidStateId.Arriving);
                return;
            }

            boid.StepFlocking(deltaTime);
        }
    }
}