using UnityEngine;

namespace IA.Agents
{
    public partial class BoidAgent
    {
        private static readonly Color SeparationGizmoColor = new Color(1f, 0.2f, 0.2f, 0.9f);
        private static readonly Color GroupGizmoColor = new Color(1f, 0.9f, 0.1f, 0.6f);
        private static readonly Color ViewGizmoColor = new Color(1f, 0.2f, 1f, 0.45f);
        private static readonly Color InterestGizmoColor = new Color(0.2f, 0.9f, 1f, 0.3f);
        private static readonly Color BodyGizmoColor = new Color(1f, 1f, 1f, 0.9f);
        private static readonly Color NeighborLineColor = new Color(0.6f, 0.75f, 1f);

        public string StateLabel
        {
            get
            {
                if (fsm == null || fsm.CurrentId == BoidStateId.Respawn) return string.Empty;
                return $"#{agentId} {StateDisplayName(fsm.CurrentId)} {RemainingHits}/{maxHits}";
            }
        }

        public Color StateLabelColor => StateTextColor(fsm != null ? fsm.CurrentId : BoidStateId.Flocking);

        private static string StateDisplayName(BoidStateId state)
        {
            switch (state)
            {
                case BoidStateId.Evade: return "EVADE";
                case BoidStateId.SeekObject: return "SEEK";
                case BoidStateId.Consume: return "CONSUME";
                case BoidStateId.Dead: return "DEAD";
                case BoidStateId.Respawn: return "RESPAWN";
                default: return "FLOCK";
            }
        }

        private static Color StateTextColor(BoidStateId state)
        {
            switch (state)
            {
                case BoidStateId.Evade: return new Color(1f, 0.45f, 0.2f);
                case BoidStateId.SeekObject: return new Color(0.4f, 1f, 0.4f);
                case BoidStateId.Consume: return new Color(1f, 0.85f, 0.1f);
                case BoidStateId.Dead: return Color.gray;
                default: return Color.white;
            }
        }

        private void UpdateVisual()
        {
            if (bodyRenderer == null) return;

            Material material = MaterialForState(fsm.CurrentId);
            if (material != null) bodyRenderer.sharedMaterial = material;
        }

        private Material MaterialForState(BoidStateId state)
        {
            switch (state)
            {
                case BoidStateId.Evade: return evadingMaterial;
                case BoidStateId.SeekObject: return seekObjectMaterial != null ? seekObjectMaterial : interactingMaterial;
                case BoidStateId.Consume: return interactingMaterial;
                case BoidStateId.Dead: return deadMaterial;
                default: return flockingMaterial;
            }
        }

        private void DrawDebugLines()
        {
            if (!showDebugLines || !IsAlive) return;

            Vector3 origin = transform.position + Vector3.up * 0.8f;

            if (detectedHunter != null) Debug.DrawLine(origin, detectedHunter.transform.position + Vector3.up, Color.red);
            if (targetInterest != null) Debug.DrawLine(origin, targetInterest.transform.position, Color.green);

            for (int i = 0; i < neighbors.Count; i++)
                Debug.DrawLine(origin, neighbors[i].transform.position + Vector3.up * 0.8f, NeighborLineColor);
        }

        private void OnDrawGizmos()
        {
            if (!showRadiiGizmos) return;
            if (Application.isPlaying && fsm != null && fsm.CurrentId == BoidStateId.Respawn) return;

            DrawPerceptionGizmos();
        }

        private void OnDrawGizmosSelected()
        {
            DrawPerceptionGizmos();

            Gizmos.color = InterestGizmoColor;
            Gizmos.DrawWireSphere(transform.position, interestSightRadius);

            Gizmos.color = BodyGizmoColor;
            Gizmos.DrawWireSphere(transform.position, minSeparationDistance * 0.5f);
        }

        private void DrawPerceptionGizmos()
        {
            Gizmos.color = SeparationGizmoColor;
            Gizmos.DrawWireSphere(transform.position, separationRadius);

            Gizmos.color = GroupGizmoColor;
            Gizmos.DrawWireSphere(transform.position, perceptionRadius);

            Gizmos.color = ViewGizmoColor;
            Gizmos.DrawWireSphere(transform.position, viewRadius);
        }
    }
}