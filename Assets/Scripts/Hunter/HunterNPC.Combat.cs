using UnityEngine;

namespace IA.Agents
{
    public partial class HunterNPC
    {
        private const float MeleePulseDuration = 0.3f;

        public void PerformMeleeAttack(BoidAgent target)
        {
            if (target == null || !IsAttackReady) return;

            ResetAttackTimer();
            PlayMeleeFeedback(target);
            target.TakeHits(meleeHits);
        }

        public void PerformRangedAttack(BoidAgent target)
        {
            if (target == null || !IsAttackReady) return;

            ResetAttackTimer();

            if (projectilePrefab == null)
            {
                target.TakeHits(rangedHits);
                return;
            }

            FireProjectile(target);
        }

        private void FireProjectile(BoidAgent target)
        {
            Vector3 origin = transform.position + Vector3.up * 0.6f + transform.forward * 0.9f;
            Vector3 direction = AgentMath.FlatDirection(origin, target.transform.position);
            if (direction == Vector3.zero) direction = transform.forward;

            GameObject projectile = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(direction));
            HunterProjectile component = projectile.GetComponent<HunterProjectile>();
            if (component != null) component.Initialize(target, projectileSpeed, rangedHits);
        }

        private void PlayMeleeFeedback(BoidAgent target)
        {
            meleePulse = MeleePulseDuration;
            lastMeleeTarget = target;
            lastMeleeTime = Time.time;
        }

        private void ResetAttackTimer()
        {
            attackTimer = 0f;
        }

        private void TickAttackTimer(float deltaTime)
        {
            attackTimer += deltaTime;
        }

        private void TickMeleePulse(float deltaTime)
        {
            meleePulse = Mathf.Max(0f, meleePulse - deltaTime);
        }
    }
}