namespace IA.Agents
{
    public class HunterHuntState : HunterState
    {
        public HunterHuntState(HunterNPC npc) : base(npc) { }

        public override HunterStateId Id => HunterStateId.Hunt;

        public override void Enter()
        {
            npc.LockPrey(npc.SelectPrey());
        }

        public override void Exit()
        {
            npc.ReleasePrey();
        }

        public override bool TryGetTransition(out HunterStateId next)
        {
            if (npc.IsEnergyDepleted) return GoTo(HunterStateId.Rest, out next);
            if (npc.DetectedDeadBoid != null) return GoTo(HunterStateId.Gather, out next);
            if (npc.HasPreyLeftArena) return GoTo(HunterStateId.Patrol, out next);
            return Stay(out next);
        }

        public override void Execute(float deltaTime)
        {
            npc.DrainEnergy(deltaTime);

            if (!npc.HasLivePrey) ReplaceDeadPrey();

            if (npc.HasLivePrey) ChaseAndAttack(npc.CurrentPrey);
            else MoveToLastKnownPosition();
        }

        private void ReplaceDeadPrey()
        {
            npc.ReleasePrey();

            BoidAgent newPrey = npc.SelectPrey();
            if (newPrey != null) npc.LockPrey(newPrey);
        }

        private void ChaseAndAttack(BoidAgent prey)
        {
            npc.RememberPreyPosition();

            float distance = AgentMath.FlatDistance(npc.Position, prey.Position);

            TryAttack(prey, distance);
            ApproachPrey(prey, distance);
        }

        private void TryAttack(BoidAgent prey, float distance)
        {
            if (!npc.IsAttackReady) return;

            if (distance <= npc.MeleeAttackRadius) npc.PerformMeleeAttack(prey);
            else if (distance <= npc.RangeAttackRadius) npc.PerformRangedAttack(prey);
        }

        private void ApproachPrey(BoidAgent prey, float distance)
        {
            if (distance <= npc.MeleeStopDistance) npc.StopMoving();
            else npc.Pursue(prey);
        }

        private void MoveToLastKnownPosition()
        {
            if (npc.HasReached(npc.LastKnownPreyPosition, npc.LastKnownPositionTolerance)) npc.StopMoving();
            else npc.MoveTowards(npc.LastKnownPreyPosition);
        }
    }
}