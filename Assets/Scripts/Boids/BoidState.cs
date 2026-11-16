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
        public abstract void Tick(float deltaTime);
        public virtual void Exit() { }

        protected bool TryDie()
        {
            if (!boid.IsOutOfHits) return false;
            boid.FSM.ChangeState(BoidStateId.Dead);
            return true;
        }

        protected bool TryEvade()
        {
            if (boid.DetectedHunter == null) return false;
            boid.RefreshThreatMemory(boid.DetectedHunter.transform.position);
            boid.FSM.ChangeState(BoidStateId.Evading);
            return true;
        }
    }
}