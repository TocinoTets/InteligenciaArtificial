namespace IA.Agents
{
    public class HunterRestState : HunterState
    {
        public HunterRestState(HunterNPC npc) : base(npc) { }

        public override HunterStateId Id => HunterStateId.Rest;

        public override bool TryGetTransition(out HunterStateId next)
        {
            if (npc.IsEnergyFull) return GoTo(HunterStateId.Patrol, out next);
            return Stay(out next);
        }

        public override void Execute(float deltaTime)
        {
            npc.StopMoving();
            npc.RechargeEnergy(deltaTime);
        }
    }
}