using UnityEngine;

namespace IA.Agents
{
    public class InterestObject : MonoBehaviour
    {
        private const float MinScaleFactor = 0.45f;

        [SerializeField] private float maxHealth = 100f;
        [Tooltip("0 = sin caducidad: sólo lo destruyen los Boids.")]
        [SerializeField] private float lifetime = 0f;
        [Tooltip("Radio (m) que ocupa el objeto a tamaño completo. Los Boids lo usan para no superponerse.")]
        [SerializeField] private float footprintRadius = 0.6f;

        private float age;
        private Vector3 initialScale;
        private float scaleFactor = 1f;

        public static int ActiveCount { get; private set; }

        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        public float HealthNormalized => maxHealth <= 0f ? 0f : Mathf.Clamp01(CurrentHealth / maxHealth);
        public bool IsAvailable => CurrentHealth > 0f;
        public float FootprintRadius => footprintRadius * scaleFactor;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetActiveCount()
        {
            ActiveCount = 0;
        }

        private void Awake()
        {
            CurrentHealth = maxHealth;
            initialScale = transform.localScale;
        }

        private void OnEnable()
        {
            ActiveCount++;
        }

        private void OnDisable()
        {
            ActiveCount = Mathf.Max(0, ActiveCount - 1);
        }

        private void Update()
        {
            TickLifetime(Time.deltaTime);
        }

        public bool ApplyDamage(float amount)
        {
            if (!IsAvailable) return false;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - Mathf.Max(0f, amount));

            if (!IsAvailable)
            {
                Destroy(gameObject);
                return true;
            }

            UpdateScale();
            return false;
        }

        private void UpdateScale()
        {
            scaleFactor = Mathf.Lerp(MinScaleFactor, 1f, HealthNormalized);
            transform.localScale = initialScale * scaleFactor;
        }

        private void TickLifetime(float deltaTime)
        {
            if (lifetime <= 0f) return;

            age += deltaTime;
            if (age >= lifetime) Destroy(gameObject);
        }
    }
}