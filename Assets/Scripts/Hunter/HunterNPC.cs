using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace IA.Agents
{
    [RequireComponent(typeof(Collider))]
    public partial class HunterNPC : MonoBehaviour
    {
        private const float InterestObjectY = 0.8f;

        [Header("Variables obligatorias")]
        [Tooltip("TBA - Time Between Attacks: tiempo mínimo entre ataques (segundos).")]
        [SerializeField] private float tba = 3f;
        [Tooltip("RangeAttackRadius: distancia máxima para un ataque a distancia (1 golpe).")]
        [SerializeField] private float rangeAttackRadius = 6f;
        [Tooltip("MeleeAttackRadius: distancia máxima para un ataque cuerpo a cuerpo (2 golpes). Menor que RangeAttackRadius.")]
        [SerializeField] private float meleeAttackRadius = 2.6f;
        [Tooltip("VisionRadius: rango de percepción para detectar Boids vivos y eliminados.")]
        [FormerlySerializedAs("perceptionRadius")]
        [SerializeField] private float visionRadius = 10f;
        [Tooltip("MaxEnergy: energía máxima del Cazador.")]
        [SerializeField] private float maxEnergy = 100f;
        [Tooltip("Energy: energía actual del Cazador. Al iniciar se llena hasta MaxEnergy.")]
        [SerializeField] private float energy = 100f;

        [Header("Energía")]
        [Tooltip("Energía que consume por segundo mientras está en Hunt.")]
        [SerializeField] private float huntEnergyDrainPerSecond = 8f;
        [Tooltip("Energía que recarga por segundo mientras está en Rest.")]
        [SerializeField] private float restEnergyRechargePerSecond = 25f;

        [Header("Movimiento")]
        [SerializeField] private float moveSpeed = 10f;
        [SerializeField] private float acceleration = 16f;
        [Tooltip("Multiplicador de la aceleración al frenar.")]
        [SerializeField] private float brakeMultiplier = 1.4f;
        [SerializeField] private float turnSpeed = 7f;
        [Tooltip("Máximo de segundos que Pursuit anticipa la posición futura de la presa.")]
        [SerializeField] private float pursuitMaxPrediction = 1.5f;

        [Header("Patrol: waypoints")]
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private float waypointTolerance = 1.2f;
        [SerializeField] private WaypointLoopMode loopMode = WaypointLoopMode.PingPong;

        [Header("Hunt")]
        [Tooltip("Distancia a la que se detiene frente a la presa.")]
        [SerializeField] private float meleeStopDistance = 1.7f;
        [Tooltip("Distancia para considerar alcanzada la última posición conocida de la presa.")]
        [SerializeField] private float lastKnownPositionTolerance = 1f;
        [Tooltip("Golpes que aplica el ataque cuerpo a cuerpo.")]
        [SerializeField] private int meleeHits = 2;
        [Tooltip("Golpes que aplica el ataque a distancia.")]
        [SerializeField] private int rangedHits = 1;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float projectileSpeed = 18f;

        [Header("Objetos de interés (generados en Patrol)")]
        [SerializeField] private GameObject interestObjectPrefab;
        [SerializeField] private float interestSpawnInterval = 5f;
        [SerializeField] private int maxActiveInterestObjects = 5;
        [SerializeField] private float spawnAreaHalfExtent = 20f;

        [Header("Gather")]
        [Tooltip("Duración de la acción de recolección.")]
        [SerializeField] private float gatherDuration = 2.5f;
        [SerializeField] private float gatherRadius = 1.8f;

        [Header("Escenario")]
        [SerializeField] private float arenaHalfExtent = 22f;

        [Header("Feedback visual")]
        [SerializeField] private bool showDebugLines = true;
        [SerializeField] private Color patrolColor = new Color(0.25f, 0.7f, 1f);
        [FormerlySerializedAs("attackColor")]
        [SerializeField] private Color huntColor = new Color(1f, 0.15f, 0.15f);
        [SerializeField] private Color gatherColor = new Color(0.85f, 0.3f, 0.95f);
        [SerializeField] private Color restColor = new Color(1f, 0.85f, 0.2f);

        private readonly List<BoidAgent> detectedBoids = new List<BoidAgent>(16);
        private readonly Collider[] overlapBuffer = new Collider[64];

        private HunterFSM fsm;
        private Collider bodyCollider;
        private Renderer bodyRenderer;
        private MaterialPropertyBlock materialProperties;
        private BoidAgent detectedDeadBoid;
        private BoidAgent closestVisibleBoid;
        private BoidAgent lastMeleeTarget;
        private Vector3 velocity;
        private float attackTimer;
        private float groundY;
        private float meleePulse;
        private float lastMeleeTime = -10f;
        private Vector3 baseScale = Vector3.one;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public HunterFSM FSM => fsm;
        public HunterStateId State => fsm.CurrentId;
        public Vector3 Position => transform.position;
        public Vector3 Velocity => velocity;

        public float TBA => tba;
        public float RangeAttackRadius => rangeAttackRadius;
        public float MeleeAttackRadius => meleeAttackRadius;
        public float VisionRadius => visionRadius;
        public float Energy => energy;
        public float MaxEnergy => maxEnergy;
        public float MeleeStopDistance => meleeStopDistance;
        public float LastKnownPositionTolerance => lastKnownPositionTolerance;
        public float GatherDuration => gatherDuration;
        public float GatherRadius => gatherRadius;
        public float InterestSpawnInterval => interestSpawnInterval;
        public int MaxActiveInterestObjects => maxActiveInterestObjects;
        public Transform[] Waypoints => waypoints;
        public float WaypointTolerance => waypointTolerance;
        public WaypointLoopMode LoopMode => loopMode;

        public IReadOnlyList<BoidAgent> DetectedBoids => detectedBoids;
        public BoidAgent DetectedDeadBoid => detectedDeadBoid;
        public BoidAgent ClosestVisibleBoid => closestVisibleBoid;
        public float AttackTimer => attackTimer;
        public bool IsAttackReady => attackTimer >= tba;

        public float ActionProgress { get; set; }

        public void Configure(Transform[] newWaypoints, GameObject newInterestObjectPrefab, GameObject newProjectilePrefab, float newArenaHalfExtent)
        {
            waypoints = newWaypoints;
            interestObjectPrefab = newInterestObjectPrefab;
            projectilePrefab = newProjectilePrefab;
            arenaHalfExtent = newArenaHalfExtent;
            spawnAreaHalfExtent = Mathf.Max(4f, newArenaHalfExtent - 2f);
        }

        private void Awake()
        {
            bodyCollider = GetComponent<Collider>();
            bodyRenderer = GetComponentInChildren<Renderer>();
            materialProperties = new MaterialPropertyBlock();
            groundY = transform.position.y;
            baseScale = transform.localScale;
            energy = maxEnergy;
            attackTimer = 0f;

            fsm = new HunterFSM();
            fsm.Register(new HunterPatrolState(this));
            fsm.Register(new HunterHuntState(this));
            fsm.Register(new HunterGatherState(this));
            fsm.Register(new HunterRestState(this));
            fsm.Start(HunterStateId.Patrol);
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            TickAttackTimer(dt);
            TickMeleePulse(dt);

            Sense();
            fsm.Tick(dt);

            UpdateVisual();
            DrawDebugLines();
        }

        private void OnValidate()
        {
            tba = Mathf.Max(0f, tba);
            rangeAttackRadius = Mathf.Max(0.2f, rangeAttackRadius);
            meleeAttackRadius = Mathf.Clamp(meleeAttackRadius, 0.1f, rangeAttackRadius - 0.1f);
            visionRadius = Mathf.Max(0.1f, visionRadius);
            maxEnergy = Mathf.Max(1f, maxEnergy);
            energy = Mathf.Clamp(energy, 0f, maxEnergy);
            huntEnergyDrainPerSecond = Mathf.Max(0f, huntEnergyDrainPerSecond);
            restEnergyRechargePerSecond = Mathf.Max(0.01f, restEnergyRechargePerSecond);
            meleeStopDistance = Mathf.Clamp(meleeStopDistance, 0.1f, meleeAttackRadius);
            meleeHits = Mathf.Max(1, meleeHits);
            rangedHits = Mathf.Max(1, rangedHits);
        }
    }
}