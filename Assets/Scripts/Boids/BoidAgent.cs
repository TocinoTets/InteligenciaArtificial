using System.Collections.Generic;
using UnityEngine;

namespace IA.Agents
{
    [RequireComponent(typeof(Collider))]
    public partial class BoidAgent : MonoBehaviour
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
        [Tooltip("Memoria de huida: segundos que el boid sigue escapando tras perder de vista al Cazador.")]
        [SerializeField] private float evadeMemoryDuration = 2.5f;
        [Tooltip("Distancia (m) a la que el boid da la huida por terminada: la fuerza se desvanece entre viewRadius y este valor.")]
        [SerializeField] private float evadeSafeDistance = 18f;

        [Header("Anti-superposición")]
        [SerializeField] private float minSeparationDistance = 1.4f;

        [Header("Vida e interacción")]
        [Tooltip("Golpes que soporta el Boid. Melee = 2 golpes, a distancia = 1 golpe.")]
        [SerializeField] private int maxHits = 2;
        [SerializeField] private float interactionDistance = 2f;
        [SerializeField] private float interactionInterval = 1f;
        [SerializeField] private float interactionDamage = 25f;
        [SerializeField] private float respawnDelay = 6f;

        [Header("Escenario")]
        [SerializeField] private float arenaHalfExtent = 22f;

        [Header("Feedback visual")]
        [SerializeField] private bool showDebugLines = true;
        [SerializeField] private Material flockingMaterial;
        [SerializeField] private Material evadingMaterial;
        [SerializeField] private Material interactingMaterial;
        [SerializeField] private Material deadMaterial;

        private readonly List<BoidAgent> neighbors = new List<BoidAgent>(16);
        private readonly List<BoidAgent> deadNeighbors = new List<BoidAgent>(8);
        private readonly List<InterestObject> visibleInterests = new List<InterestObject>(8);
        private readonly Collider[] overlapBuffer = new Collider[64];

        private Collider bodyCollider;
        private Renderer bodyRenderer;
        private HunterNPC detectedHunter;
        private Vector3 lastThreatPosition;
        private InterestObject targetInterest;
        private BoidFSM fsm;
        private Vector3 velocity;
        private int hitsTaken;
        private float interactionTimer;
        private float respawnTimer;
        private float wanderAngle;
        private float groundY;
        private float evadeMemoryTimer;

        public int AgentId => agentId;
        public BoidFSM FSM => fsm;
        public Vector3 Position => transform.position;
        public BoidStateId State => fsm.CurrentId;
        public int HitsTaken => hitsTaken;
        public int MaxHits => maxHits;
        public int RemainingHits => Mathf.Max(0, maxHits - hitsTaken);
        public Vector3 Velocity => velocity;
        public bool IsAlive => fsm.CurrentId != BoidStateId.Dead && fsm.CurrentId != BoidStateId.Respawning;
        public bool IsHarvestable => fsm.CurrentId == BoidStateId.Dead;
        public HunterNPC DetectedHunter => detectedHunter;
        public InterestObject TargetInterest => targetInterest;
        public IReadOnlyList<BoidAgent> Neighbors => neighbors;

        internal float InteractionDistance => interactionDistance;
        internal float InterestSightRadius => interestSightRadius;
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

            fsm = new BoidFSM();
            fsm.Register(new BoidFlockingState(this));
            fsm.Register(new BoidEvadingState(this));
            fsm.Register(new BoidArrivingState(this));
            fsm.Register(new BoidInteractingState(this));
            fsm.Register(new BoidDeadState(this));
            fsm.Register(new BoidRespawningState(this));
            fsm.ChangeState(BoidStateId.Flocking);
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (fsm.CurrentId == BoidStateId.Dead || fsm.CurrentId == BoidStateId.Respawning)
            {
                fsm.Tick(dt);
                DrawDebugLines();
                return;
            }

            Sense();
            fsm.Tick(dt);
            UpdateVisual();
            DrawDebugLines();
        }
    }
}