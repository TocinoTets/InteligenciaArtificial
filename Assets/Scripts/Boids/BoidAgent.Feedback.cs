using UnityEngine;

namespace IA.Agents
{

    public partial class BoidAgent
    {
        private void UpdateVisual()
        {
            if (bodyRenderer == null) return;

            BoidStateId current = fsm.CurrentId;
            Material material = MaterialForState(current);
            if (material != null) bodyRenderer.sharedMaterial = material;

            if (current != BoidStateId.Arriving)
            {
                bodyRenderer.SetPropertyBlock(null);
                return;
            }

            tintBlock ??= new MaterialPropertyBlock();
            Material shared = bodyRenderer.sharedMaterial;
            tintBlock.SetColor(shared != null && shared.HasProperty("_BaseColor") ? "_BaseColor" : "_Color", seekColor);
            bodyRenderer.SetPropertyBlock(tintBlock);
        }

        private Material MaterialForState(BoidStateId current)
        {
            switch (current)
            {
                case BoidStateId.Evading: return evadingMaterial;
                case BoidStateId.Arriving:
                case BoidStateId.Interacting: return interactingMaterial;
                case BoidStateId.Dead: return deadMaterial;
                default: return flockingMaterial;
            }
        }

        private void DrawDebugLines()
        {
            if (!showDebugLines) return;

            Vector3 origin = transform.position + Vector3.up * 0.8f;

            if (detectedHunter != null) Debug.DrawLine(origin, detectedHunter.transform.position + Vector3.up, Color.red);
            if (targetInterest != null) Debug.DrawLine(origin, targetInterest.transform.position, Color.green);

            for (int i = 0; i < neighbors.Count; i++)
                Debug.DrawLine(origin, neighbors[i].transform.position + Vector3.up * 0.8f, new Color(0.6f, 0.75f, 1f));
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            Gizmos.color = new Color(1f, 0.35f, 0.35f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, separationRadius);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, separationRadius);

            Gizmos.color = new Color(1f, 0.9f, 0.1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, perceptionRadius);

            Gizmos.color = new Color(1f, 0.2f, 1f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, viewRadius);

            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, interestSightRadius);

            Gizmos.color = new Color(1f, 1f, 1f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, minSeparationDistance);
        }
    }
}