namespace IA.Agents
{
    public abstract class BoidState
    {
        protected readonly BoidAgent boid;

        protected BoidState(BoidAgent boid)
        {
            this.boid = boid;
        }

        public abstract BoidStateId Id { get; }

        public virtual void Enter() { }

        public virtual void Exit() { }

        public abstract bool TryGetTransition(out BoidStateId next);

        public abstract void Execute(float deltaTime);

        protected BoidStateId ResolveCalmState()
        {
            if (boid.IsTargetInReach) return BoidStateId.Consume;
            if (boid.HasValidTarget || boid.SeesInterest) return BoidStateId.SeekObject;
            return BoidStateId.Flocking;
        }

        protected bool Stay(out BoidStateId next)
        {
            next = Id;
            return false;
        }

        protected static bool GoTo(BoidStateId target, out BoidStateId next)
        {
            next = target;
            return true;
        }
    }
}