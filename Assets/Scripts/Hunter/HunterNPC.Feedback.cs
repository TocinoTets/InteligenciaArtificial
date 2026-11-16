using UnityEngine;

namespace IA.Agents
{
    public partial class HunterNPC
    {
        private const float MeleeFlashDuration = 0.35f;
        private static readonly Color RangeGizmoColor = new Color(1f, 0.6f, 0.1f, 0.5f);

        public string StateLabel
        {
            get
            {
                if (fsm == null) return string.Empty;
                return $"{StateDetail()}\n{EnergyText()} | {AttackTimerText()}";
            }
        }

        public Color StateLabelColor => StateColor(fsm != null ? fsm.CurrentId : HunterStateId.Patrol);

        private string StateDetail()
        {
            switch (fsm.CurrentId)
            {
                case HunterStateId.Hunt:
                    return currentPrey != null ? $"HUNT -> Boid #{currentPrey.AgentId}" : "HUNT -> última posición";
                case HunterStateId.Gather:
                    return $"GATHER {ActionProgress * 100f:0}%";
                case HunterStateId.Rest:
                    return "REST";
                default:
                    return "PATROL";
            }
        }

        private string EnergyText()
        {
            return $"E {energy:0}/{maxEnergy:0}";
        }

        private string AttackTimerText()
        {
            return IsAttackReady ? "TBA listo" : $"TBA {Mathf.Max(0f, tba - attackTimer):0.0}s";
        }

        public Color StateColor(HunterStateId id)
        {
            switch (id)
            {
                case HunterStateId.Hunt: return huntColor;
                case HunterStateId.Gather: return gatherColor;
                case HunterStateId.Rest: return restColor;
                default: return patrolColor;
            }
        }

        private void UpdateVisual()
        {
            ApplyStateColor();
            ApplyMeleePulse();
        }

        private void ApplyStateColor()
        {
            if (bodyRenderer == null || materialProperties == null) return;

            Material material = bodyRenderer.sharedMaterial;
            if (material == null) return;

            int propertyId = material.HasProperty(BaseColorId) ? BaseColorId : ColorId;
            materialProperties.SetColor(propertyId, StateColor(fsm.CurrentId));
            bodyRenderer.SetPropertyBlock(materialProperties);
        }

        private void ApplyMeleePulse()
        {
            float pulse = meleePulse > 0f ? Mathf.Sin(Mathf.Clamp01(meleePulse / MeleePulseDuration) * Mathf.PI) : 0f;
            transform.localScale = baseScale * (1f + 0.25f * pulse);
        }

        private void DrawDebugLines()
        {
            if (!showDebugLines) return;

            Vector3 origin = transform.position + Vector3.up;

            DrawPreyLine(origin);
            DrawDeadBoidLine(origin);
            DrawWaypointPath();
            DrawMeleeFlash(origin);
        }

        private void DrawPreyLine(Vector3 origin)
        {
            if (fsm.CurrentId != HunterStateId.Hunt) return;

            Vector3 target = currentPrey != null ? currentPrey.Position : lastKnownPreyPosition;
            Debug.DrawLine(origin, target + Vector3.up, huntColor);
        }

        private void DrawDeadBoidLine(Vector3 origin)
        {
            if (detectedDeadBoid == null) return;
            Debug.DrawLine(origin, detectedDeadBoid.transform.position + Vector3.up, gatherColor);
        }

        private void DrawWaypointPath()
        {
            if (waypoints == null || waypoints.Length < 2) return;

            for (int i = 0; i < waypoints.Length; i++)
            {
                Transform current = waypoints[i];
                Transform next = waypoints[(i + 1) % waypoints.Length];
                if (current != null && next != null) Debug.DrawLine(current.position, next.position, patrolColor);
            }
        }

        private void DrawMeleeFlash(Vector3 origin)
        {
            if (lastMeleeTarget == null || Time.time - lastMeleeTime >= MeleeFlashDuration) return;
            Debug.DrawLine(origin, lastMeleeTarget.transform.position + Vector3.up, Color.white);
        }

        private void OnDrawGizmos()
        {
            DrawRanges();
        }

        private void DrawRanges()
        {
            Gizmos.color = new Color(patrolColor.r, patrolColor.g, patrolColor.b, 0.3f);
            Gizmos.DrawWireSphere(transform.position, visionRadius);

            Gizmos.color = RangeGizmoColor;
            Gizmos.DrawWireSphere(transform.position, rangeAttackRadius);

            Gizmos.color = new Color(huntColor.r, huntColor.g, huntColor.b, 0.8f);
            Gizmos.DrawWireSphere(transform.position, meleeAttackRadius);
        }
    }
}