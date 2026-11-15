using UnityEngine;

namespace IA.Agents
{
    public class HunterProjectile : MonoBehaviour
    {
        [SerializeField] private float hitRadius = 0.7f;
        [SerializeField] private float maxLifetime = 5f;

        private BoidAgent target;
        private float speed = 18f;
        private int hits = 1;
        private float life;

        public void Initialize(BoidAgent newTarget, float newSpeed, int newHits)
        {
            target = newTarget;
            speed = newSpeed;
            hits = newHits;
            life = 0f;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            life += dt;

            if (life >= maxLifetime || target == null || !target.IsAlive)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 aim = target.transform.position + Vector3.up * 0.4f;
            Vector3 toTarget = aim - transform.position;
            float distance = toTarget.magnitude;
            float step = speed * dt;

            if (distance - step <= hitRadius)
            {
                target.TakeHits(hits);
                Destroy(gameObject);
                return;
            }

            Vector3 direction = toTarget / distance;
            transform.position += direction * step;
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }
}