using UnityEngine;

namespace IA.Agents
{
    public partial class BoidAgent
    {
        private bool wasHarvested;

        public bool IsOutOfHits => hitsTaken >= maxHits;
        public bool WasHarvested => wasHarvested;
        public bool IsRespawnReady => respawnTimer >= respawnDelay;

        public void TakeHits(int hits)
        {
            if (!IsAlive || hits <= 0) return;

            hitsTaken = Mathf.Min(maxHits, hitsTaken + hits);
        }

        public void MarkAsHarvested()
        {
            if (!IsHarvestable) return;

            wasHarvested = true;
        }

        internal void StopForDeath()
        {
            velocity = Vector3.zero;
            SetTargetInterest(null);
        }

        internal void BeginRespawn()
        {
            wasHarvested = false;
            respawnTimer = 0f;
            velocity = Vector3.zero;
            SetVisible(false);
        }

        internal void TickRespawnTimer(float dt)
        {
            respawnTimer += dt;
        }

        internal void RespawnAtRandomPosition()
        {
            float half = Mathf.Max(2f, arenaHalfExtent - 2f);
            transform.position = AgentMath.RandomPointInSquare(half, groundY);

            hitsTaken = 0;
            velocity = Vector3.zero;
            SetVisible(true);
        }

        internal void ResetInteractionTimer()
        {
            interactionTimer = 0f;
        }

        internal void TickInteraction(float dt)
        {
            if (!HasValidTarget) return;

            interactionTimer += dt;
            if (interactionTimer < interactionInterval) return;

            interactionTimer = 0f;
            targetInterest.ApplyDamage(interactionDamage);
        }

        private void SetVisible(bool visible)
        {
            if (bodyRenderer != null) bodyRenderer.enabled = visible;
            if (bodyCollider != null) bodyCollider.enabled = visible;
        }
    }
}