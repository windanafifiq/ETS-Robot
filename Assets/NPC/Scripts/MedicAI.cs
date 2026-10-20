using UnityEngine;
using UnityEngine.AI;
using RobotArena; 
using RobotArena.Environment;
using RobotArena.DebugTools;

namespace RobotArena.Enemy
{
    public enum MedicState { Idle, Healing, Fleeing, Hiding, Dead }

    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(RobotPerception))]
    [RequireComponent(typeof(HealthSystem))]
    public class MedicAI : MonoBehaviour, IDebugInfo
    {
        [Header("Mencari Sekutu")]
        [SerializeField] private LayerMask allyLayer;
        [SerializeField] private float searchRadius = 25f;

        [Header("Menyembuhkan")]
        [SerializeField] private float healRange = 3f;
        [SerializeField] private int healAmount = 8;
        [SerializeField] private float healCooldown = 1f;
        [SerializeField, Range(0.1f, 1f)] private float healThreshold = 0.8f;

        [Header("Menjaga Diri")]
        [SerializeField] private float safeDistance = 9f;
        [SerializeField] private float fleeStep = 8f;
        [SerializeField] private float recentDamageWindow = 2f;

        [Header("Bobot Utility")]
        [SerializeField] private float healWeight = 1f;
        [SerializeField] private float fleeWeight = 1.2f;
        [SerializeField] private float coverWeight = 1f;

        [Header("Timing")]
        [SerializeField] private float decisionInterval = 0.4f;

        public MedicState State { get; private set; } = MedicState.Idle;

        private NavMeshAgent agent;
        private RobotPerception perception;
        private HealthSystem health;

        private HealthSystem healTarget;
        private CoverPoint reservedCover;
        private NpcPatrol patrol;

        private float decisionTimer;
        private float healTimer;
        private float recentDamageTimer;

        private float lastHealScore, lastFleeScore, lastCoverScore;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            perception = GetComponent<RobotPerception>();
            health = GetComponent<HealthSystem>();

            patrol = GetComponent<NpcPatrol>();
            if (patrol == null) patrol = gameObject.AddComponent<NpcPatrol>();
        }

        private void OnEnable()
        {
            health.OnDamaged += HandleDamaged;
            health.OnDeath += HandleDeath;
        }

        private void OnDisable()
        {
            health.OnDamaged -= HandleDamaged;
            health.OnDeath -= HandleDeath;
        }

        private void Update()
        {
            if (State == MedicState.Dead) return;

            decisionTimer -= Time.deltaTime;
            healTimer -= Time.deltaTime;
            recentDamageTimer -= Time.deltaTime;

            if (decisionTimer <= 0f) { Decide(); decisionTimer = decisionInterval; }
            if (State == MedicState.Healing) TickHealing();
            if (State == MedicState.Idle) patrol.Tick(agent);   // Medic berpatroli saat tidak ada yang disembuhkan
        }

        private void Decide()
        {
            healTarget = FindMostWoundedAlly();

            float healScore = 0f;
            if (healTarget != null) healScore = (0.3f + (1f - healTarget.HPPercent)) * healWeight;

            float fleeScore = 0f;
            if (perception.CanSeePlayer && perception.DistanceToPlayer < safeDistance)
                fleeScore = (1f - perception.DistanceToPlayer / safeDistance) * fleeWeight;

            Vector3 threat = GetThreatPosition();
            bool coverAvailable = perception.HasMemoryOfPlayer &&
                CoverPointManager.Instance != null &&
                CoverPointManager.Instance.FindNearestAvailableCover(transform.position, threat, transform) != null;

            float coverScore = (recentDamageTimer > 0f && coverAvailable) ? 0.9f * coverWeight : 0f;
            float idleScore = 0.05f;

            lastHealScore = healScore; lastFleeScore = fleeScore; lastCoverScore = coverScore;

            float best = idleScore;
            MedicState chosen = MedicState.Idle;

            if (healScore > best)  { best = healScore;  chosen = MedicState.Healing; }
            if (fleeScore > best)  { best = fleeScore;  chosen = MedicState.Fleeing; }
            if (coverScore > best) { best = coverScore; chosen = MedicState.Hiding; }

            if (chosen == MedicState.Idle && State != MedicState.Idle) patrol.Begin();
            State = chosen;
            Execute(chosen, threat);
        }

        private void Execute(MedicState chosen, Vector3 threat)
        {
            if (chosen != MedicState.Hiding) ReleaseCover();

            switch (chosen)
            {
                case MedicState.Healing:
                    float d = Vector3.Distance(transform.position, healTarget.transform.position);
                    if (d > healRange * 0.8f) NavUtil.TryMoveTo(agent, healTarget.transform.position);
                    else agent.ResetPath();
                    break;
                case MedicState.Fleeing:
                    Vector3 away = (transform.position - threat).normalized;
                    NavUtil.TryMoveTo(agent, transform.position + away * fleeStep);
                    break;
                case MedicState.Hiding:
                    CoverPoint cp = CoverPointManager.Instance.FindNearestAvailableCover(transform.position, threat, transform);
                    if (cp != null)
                    {
                        if (reservedCover != null && reservedCover != cp) reservedCover.Release(transform);
                        cp.Reserve(transform);
                        reservedCover = cp;
                        NavUtil.TryMoveTo(agent, cp.transform.position);
                    }
                    break;
                case MedicState.Idle:
                    break; // berpatroli lewat patrol.Tick() di Update
            }
        }

        private void TickHealing()
        {
            if (healTarget == null || healTarget.IsDead) return;
            Vector3 targetPos = healTarget.transform.position;
            float d = Vector3.Distance(transform.position, targetPos);
            FaceTarget(targetPos);

            if (d <= healRange && healTimer <= 0f)
            {
                healTarget.Heal(healAmount);
                healTimer = healCooldown;
            }
        }

        private HealthSystem FindMostWoundedAlly()
        {
            Collider[] cols = Physics.OverlapSphere(transform.position, searchRadius, allyLayer);
            HealthSystem best = null;
            float lowest = healThreshold;

            foreach (Collider c in cols)
            {
                HealthSystem hs = c.GetComponentInParent<HealthSystem>();
                if (hs == null || hs == health || hs.IsDead || hs.Team != health.Team) continue;

                if (hs.HPPercent < lowest)
                {
                    lowest = hs.HPPercent;
                    best = hs;
                }
            }
            return best;
        }

        private Vector3 GetThreatPosition() => perception.CanSeePlayer && perception.PlayerTransform != null ? perception.PlayerTransform.position : perception.LastKnownPlayerPosition;

        private void FaceTarget(Vector3 worldPos)
        {
            Vector3 flat = new Vector3(worldPos.x, transform.position.y, worldPos.z);
            if ((flat - transform.position).sqrMagnitude > 0.01f) transform.LookAt(flat);
        }

        private void ReleaseCover()
        {
            if (reservedCover == null) return;
            reservedCover.Release(transform);
            reservedCover = null;
        }

        private void HandleDamaged() => recentDamageTimer = recentDamageWindow;

        private void HandleDeath()
        {
            State = MedicState.Dead;
            ReleaseCover();
            agent.isStopped = true;
            foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;
            enabled = false;
            Destroy(gameObject, 2f);
        }

        public string GetDebugText()
        {
            return $"MEDIC: {State}\nH:{lastHealScore:F2} F:{lastFleeScore:F2} C:{lastCoverScore:F2}\nHP:{health.CurrentHP}/{health.MaxHP}";
        }
    }
}