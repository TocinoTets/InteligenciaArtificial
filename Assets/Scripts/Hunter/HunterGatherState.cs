using UnityEngine;

namespace IA.Agents
{
    public class HunterGatherState : HunterState
    {
        private BoidAgent target;
        private float collectTimer;
        private bool completed;

        public HunterGatherState(HunterNPC npc) : base(npc) { }

        public override HunterStateId Id => HunterStateId.Gather;

        public override void Enter()
        {
            target = npc.DetectedDeadBoid;
            completed = false;
            ResetCollection();
        }

        public override void Exit()
        {
            target = null;
            ResetCollection();
        }

        public override bool TryGetTransition(out HunterStateId next)
        {
            if (completed) return GoTo(HunterStateId.Patrol, out next);
            if (target == null || !target.IsHarvestable) return GoTo(HunterStateId.Patrol, out next);
            return Stay(out next);
        }

        public override void Execute(float deltaTime)
        {
            if (!npc.HasReached(target.Position, npc.GatherRadius))
            {
                ResetCollection();
                npc.MoveTowards(target.Position);
                return;
            }

            npc.StopMoving();
            Collect(deltaTime);
        }

        private void Collect(float deltaTime)
        {
            collectTimer += deltaTime;
            npc.ActionProgress = Mathf.Clamp01(collectTimer / Mathf.Max(0.01f, npc.GatherDuration));

            if (collectTimer < npc.GatherDuration) return;

            target.Harvest();
            completed = true;
        }

        private void ResetCollection()
        {
            collectTimer = 0f;
            npc.ActionProgress = 0f;
        }
    }
}