using UnityEngine;

namespace IA.Agents
{

    public class InterestObject : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;
        [Tooltip("0 = sin caducidad: sólo lo destruyen los Boids.")]
        [SerializeField] private float lifetime = 0f;

        private float age;
        private Vector3 initialScale;

        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        public float HealthNormalized => maxHealth <= 0f ? 0f : Mathf.Clamp01(CurrentHealth / maxHealth);

        public bool IsAvailable => CurrentHealth > 0f;

        private void Awake()
        {
            CurrentHealth = maxHealth;
            initialScale = transform.localScale;
        }

        private void Update()
        {
            if (lifetime <= 0f) return;
            age += Time.deltaTime;
            if (age >= lifetime) Destroy(gameObject);
        }

        public bool ApplyDamage(float amount)
        {
            if (!IsAvailable) return false;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - Mathf.Max(0f, amount));

            if (CurrentHealth <= 0f)
            {
                Destroy(gameObject);
                return true;
            }

            transform.localScale = initialScale * Mathf.Lerp(0.45f, 1f, HealthNormalized);
            return false;
        }
    }
}
