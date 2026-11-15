namespace IA.Agents
{
    public abstract class HunterState
    {
        protected readonly HunterNPC npc;

        protected HunterState(HunterNPC npc)
        {
            this.npc = npc;
        }

        public abstract HunterStateId Id { get; }

        public virtual void Enter() { }

        public virtual void Exit() { }

        public abstract bool TryGetTransition(out HunterStateId next);

        public abstract void Execute(float deltaTime);

        protected bool Stay(out HunterStateId next)
        {
            next = Id;
            return false;
        }

        protected static bool GoTo(HunterStateId target, out HunterStateId next)
        {
            next = target;
            return true;
        }
    }
}