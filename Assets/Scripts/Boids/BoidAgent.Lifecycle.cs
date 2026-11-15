using UnityEngine;

namespace IA.Agents
{
    public partial class BoidAgent
    {
        public void TakeHits(int hits)
        {
            if (!IsAlive || hits <= 0) return;

            hitsTaken = Mathf.Min(maxHits, hitsTaken + hits);

            if (hitsTaken >= maxHits) fsm.ChangeState(BoidStateId.Dead);
        }

        internal void StopForDeath()
        {
            velocity = Vector3.zero;
            SetTargetInterest(null);
            UpdateVisual();
        }

        public bool Harvest()
        {
            if (fsm.CurrentId != BoidStateId.Dead) return false;
            fsm.ChangeState(BoidStateId.Respawning);
            return true;
        }

        internal void BeginRespawnWait()
        {
            respawnTimer = 0f;
            velocity = Vector3.zero;
            SetVisible(false);
        }

        internal void TickRespawnWait(float dt)
        {
            respawnTimer += dt;
            if (respawnTimer < respawnDelay) return;

            float half = Mathf.Max(2f, arenaHalfExtent - 2f);
            transform.position = AgentMath.RandomPointInSquare(half, groundY);

            hitsTaken = 0;
            velocity = Vector3.zero;
            SetVisible(true);
            UpdateVisual();

            fsm.ChangeState(BoidStateId.Flocking);
        }

        internal void ResetInteractionTimer() => interactionTimer = 0f;

        internal void TickInteraction(float dt)
        {
            if (targetInterest == null) return;

            interactionTimer += dt;
            if (interactionTimer < interactionInterval) return;

            interactionTimer = 0f;

            bool destroyed = targetInterest.ApplyDamage(interactionDamage);

            if (destroyed) targetInterest = null;
        }

        private void SetVisible(bool visible)
        {
            if (bodyRenderer != null) bodyRenderer.enabled = visible;
            if (bodyCollider != null) bodyCollider.enabled = visible;
        }
    }
}