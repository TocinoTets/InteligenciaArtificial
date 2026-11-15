using UnityEngine;

namespace IA.Agents
{
    public class HunterPatrolState : HunterState
    {
        private int waypointIndex;
        private int direction = 1;
        private float spawnTimer;

        public HunterPatrolState(HunterNPC npc) : base(npc) { }

        public override HunterStateId Id => HunterStateId.Patrol;

        public override void Enter()
        {
            if (HasWaypoints()) waypointIndex = Mathf.Clamp(waypointIndex, 0, npc.Waypoints.Length - 1);
        }

        public override bool TryGetTransition(out HunterStateId next)
        {
            if (npc.DetectedDeadBoid != null) return GoTo(HunterStateId.Gather, out next);
            if (npc.IsAttackReady && npc.ClosestVisibleBoid != null) return GoTo(HunterStateId.Hunt, out next);
            return Stay(out next);
        }

        public override void Execute(float deltaTime)
        {
            FollowWaypoints();
            TickInterestSpawner(deltaTime);
        }

        private void FollowWaypoints()
        {
            if (!HasWaypoints())
            {
                npc.StopMoving();
                return;
            }

            Transform waypoint = npc.Waypoints[waypointIndex];

            if (waypoint != null && npc.HasReached(waypoint.position, npc.WaypointTolerance))
            {
                AdvanceWaypoint();
                waypoint = npc.Waypoints[waypointIndex];
            }

            if (waypoint != null) npc.MoveTowards(waypoint.position);
        }

        private void AdvanceWaypoint()
        {
            int count = npc.Waypoints.Length;
            if (count == 1) return;

            if (npc.LoopMode == WaypointLoopMode.Loop)
            {
                waypointIndex = (waypointIndex + 1) % count;
                return;
            }

            if (waypointIndex + direction >= count || waypointIndex + direction < 0) direction = -direction;
            waypointIndex += direction;
        }

        private void TickInterestSpawner(float deltaTime)
        {
            spawnTimer += deltaTime;
            if (spawnTimer < npc.InterestSpawnInterval) return;

            spawnTimer = 0f;
            if (npc.ActiveInterestObjectCount() < npc.MaxActiveInterestObjects) npc.SpawnInterestObject();
        }

        private bool HasWaypoints()
        {
            return npc.Waypoints != null && npc.Waypoints.Length > 0;
        }
    }
}