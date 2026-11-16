using System.Collections.Generic;
using UnityEngine;

namespace IA.Agents
{
    [RequireComponent(typeof(Collider))]
    public partial class BoidAgent : MonoBehaviour, IStateLabelSource
    {
        [Header("Identidad")]
        [SerializeField] private int agentId = 1;

        [Header("Radios de percepción locales")]
        [Tooltip("Separation: radio MENOR y distinto al de Alignment y Cohesion.")]
        [SerializeField] private float separationRadius = 2.5f;
        [Tooltip("Alignment + Cohesion: radio de percepción del grupo.")]
        [SerializeField] private float perceptionRadius = 7f;
        [Tooltip("Rango de visión con el que detecta al NPC Cazador (dispara Evade).")]
        [SerializeField] private float viewRadius = 12f;
        [Tooltip("Alcance limitado con el que detecta los objetos de interés del escenario.")]
        [SerializeField] private float interestSightRadius = 16f;

        [Header("Movimiento")]
        [SerializeField] private float maxSpeed = 6.5f;
        [SerializeField] private float maxForce = 22f;
        [SerializeField] private float turnSpeed = 9f;

        [Header("Pesos de los comportamientos")]
        [SerializeField] private float separationWeight = 2.2f;
        [SerializeField] private float alignmentWeight = 1f;
        [SerializeField] private float cohesionWeight = 1f;
        [SerializeField] private float evadeWeight = 5f;
        [SerializeField] private float arriveWeight = 2.6f;
        [SerializeField] private float wanderWeight = 0.8f;
        [Tooltip("Multiplicador de Separation aplicado a los Boids muertos.")]
        [SerializeField] private float deadSeparationFactor = 0.6f;

        [Header("Evade")]
        [Tooltip("Máximo de segundos que se anticipa la posición futura del Cazador.")]
        [SerializeField] private float evadeMaxPrediction = 1.5f;

        [Header("Wander")]
        [Tooltip("Grados por segundo que puede variar la dirección de deambular.")]
        [SerializeField] private float wanderJitter = 60f;
        [Tooltip("Fracción de la velocidad máxima usada al deambular.")]
        [SerializeField, Range(0f, 1f)] private float wanderSpeedFactor = 0.6f;

        [Header("Seek Object / Consume")]
        [Tooltip("Distancia a la que Arrive empieza a desacelerar.")]
        [SerializeField] private float arriveSlowingRadius = 5f;
        [Tooltip("Distancia a la que Arrive termina de frenar. Menor que InteractionDistance.")]
        [SerializeField] private float arriveStopDistance = 1.6f;
        [Tooltip("Distancia máxima para empezar a consumir el objeto.")]
        [SerializeField] private float interactionDistance = 2f;
        [Tooltip("Margen extra antes de abandonar Consume, para no alternar entre estados.")]
        [SerializeField] private float consumeExitMargin = 0.5f;
        [SerializeField] private float interactionInterval = 1f;
        [SerializeField] private float interactionDamage = 25f;

        [Header("Anti-superposición")]
        [Tooltip("Distancia mínima entre centros de dos agentes.")]
        [SerializeField] private float minSeparationDistance = 1.4f;

        [Header("Vida")]
        [Tooltip("Golpes que soporta el Boid. Melee = 2 golpes, a distancia = 1 golpe.")]
        [SerializeField] private int maxHits = 2;
        [SerializeField] private float respawnDelay = 6f;

        [Header("Escenario")]
        [SerializeField] private float arenaHalfExtent = 22f;

        [Header("Feedback visual")]
        [SerializeField] private bool showDebugLines = true;
        [Tooltip("Dibuja siempre los radios de Separation, Alignment/Cohesion y visión.")]
        [SerializeField] private bool showRadiiGizmos = true;
        [Tooltip("Muestra sobre el Boid su estado y los golpes que le quedan.")]
        [SerializeField] private bool showStateLabel = true;
        [SerializeField] private Material flockingMaterial;
        [SerializeField] private Material evadingMaterial;
        [Tooltip("Material del estado Seek Object. Si está vacío usa el de Consume.")]
        [SerializeField] private Material seekObjectMaterial;
        [Tooltip("Material del estado Consume.")]
        [SerializeField] private Material interactingMaterial;
        [SerializeField] private Material deadMaterial;

        private readonly List<BoidAgent> neighbors = new List<BoidAgent>(16);
        private readonly List<BoidAgent> deadNeighbors = new List<BoidAgent>(8);
        private readonly List<InterestObject> visibleInterests = new List<InterestObject>(8);
        private readonly Collider[] overlapBuffer = new Collider[64];

        private Collider bodyCollider;
        private Renderer bodyRenderer;
        private HunterNPC detectedHunter;
        private InterestObject targetInterest;
        private BoidFSM fsm;
        private Vector3 velocity;
        private int hitsTaken;
        private float interactionTimer;
        private float respawnTimer;
        private float wanderAngle;
        private float groundY;

        public int AgentId => agentId;
        public BoidFSM FSM => fsm;
        public Vector3 Position => transform.position;
        public BoidStateId State => fsm.CurrentId;
        public int HitsTaken => hitsTaken;
        public int MaxHits => maxHits;
        public int RemainingHits => Mathf.Max(0, maxHits - hitsTaken);
        public Vector3 Velocity => velocity;
        public bool IsAlive => !IsOutOfHits && fsm.CurrentId != BoidStateId.Dead && fsm.CurrentId != BoidStateId.Respawn;
        public bool IsHarvestable => fsm.CurrentId == BoidStateId.Dead && !wasHarvested;
        public HunterNPC DetectedHunter => detectedHunter;
        public InterestObject TargetInterest => targetInterest;
        public IReadOnlyList<BoidAgent> Neighbors => neighbors;

        internal void SetTargetInterest(InterestObject value) => targetInterest = value;

        public void Configure(int newAgentId, float newArenaHalfExtent, Material newFlocking, Material newEvading, Material newInteracting, Material newDead)
        {
            agentId = newAgentId;
            arenaHalfExtent = newArenaHalfExtent;
            flockingMaterial = newFlocking;
            evadingMaterial = newEvading;
            interactingMaterial = newInteracting;
            deadMaterial = newDead;
        }

        private void Awake()
        {
            bodyCollider = GetComponent<Collider>();
            bodyRenderer = GetComponentInChildren<Renderer>();
            groundY = transform.position.y;
            hitsTaken = 0;
            wanderAngle = Random.Range(0f, 360f);

            if (showStateLabel && GetComponent<AgentStateLabel>() == null) gameObject.AddComponent<AgentStateLabel>();

            fsm = new BoidFSM(this);
            fsm.Register(new BoidFlockingState(this));
            fsm.Register(new BoidEvadeState(this));
            fsm.Register(new BoidSeekObjectState(this));
            fsm.Register(new BoidConsumeState(this));
            fsm.Register(new BoidDeadState(this));
            fsm.Register(new BoidRespawnState(this));
            fsm.Start(BoidStateId.Flocking);
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (IsAlive) Sense();
            else ClearPerception();

            fsm.Tick(dt);
            UpdateVisual();
            DrawDebugLines();
        }

        private void OnValidate()
        {
            perceptionRadius = Mathf.Max(0.2f, perceptionRadius);
            separationRadius = Mathf.Clamp(separationRadius, 0.1f, perceptionRadius - 0.1f);
            maxSpeed = Mathf.Max(0.1f, maxSpeed);
            maxForce = Mathf.Max(0.1f, maxForce);
            interactionDistance = Mathf.Max(0.2f, interactionDistance);
            arriveStopDistance = Mathf.Clamp(arriveStopDistance, 0.1f, interactionDistance - 0.05f);
            arriveSlowingRadius = Mathf.Max(arriveStopDistance + 0.1f, arriveSlowingRadius);
            consumeExitMargin = Mathf.Max(0f, consumeExitMargin);
            interactionInterval = Mathf.Max(0.05f, interactionInterval);
            minSeparationDistance = Mathf.Max(0.1f, minSeparationDistance);
            maxHits = Mathf.Max(1, maxHits);
            respawnDelay = Mathf.Max(0f, respawnDelay);
        }
    }
}