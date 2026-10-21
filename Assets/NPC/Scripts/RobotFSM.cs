using UnityEngine;
using UnityEngine.AI;
using RobotArena; 
using RobotArena.Environment;
using RobotArena.DebugTools;

namespace RobotArena.Enemy
{
    public enum RobotState { Idle, Alert, Combat, Retreat, Dead }

    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(RobotPerception))]
    [RequireComponent(typeof(HealthSystem))]
    public class RobotFSM : MonoBehaviour, IDebugInfo
    {
        [Header("Role Config")]
        [SerializeField] private RobotUtilityWeights utilityWeights;
     
        [Header("Sniper Settings")]
        [SerializeField] private bool stationarySniper = false;
        [SerializeField] private SniperZone sniperZone;

        [Header("Weapon")]
        [SerializeField] private Transform firePoint;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float weaponCooldown = 1.2f;
        [SerializeField] private int damage = 10;

        [Header("Timing")]
        [SerializeField] private float alertDelay = 0.5f;
        [SerializeField] private float decisionInterval = 0.4f;
        [SerializeField] private float retreatDistance = 10f;
        [SerializeField] private float recentDamageWindow = 1.5f;

        public RobotState CurrentState { get; private set; } = RobotState.Idle;
        public UtilityAction LastChosenAction { get; private set; }
        public (float attack, float distance, float cover, float retreat) LastUtilityScores { get; private set; }

        private NavMeshAgent agent;
        private RobotPerception perception;
        private HealthSystem health;
        private UtilityScorer scorer;
        private Animator anim; // VARIABEL ANIMATOR BARU

        [Header("Serangan Jarak Dekat (Attacker)")]
        [Tooltip("Jeda dari mulai animasi sampai pukulan mengenai (detik). Sesuaikan dengan animasimu.")]
        [SerializeField] private float meleeHitDelay = 0.35f;
        [Tooltip("Nama parameter Trigger di Animator. Satu dipilih acak tiap serangan. Isi 'Attack', atau 'Punch' dan 'Kick' kalau kamu membuatnya terpisah.")]
        [SerializeField] private string[] attackTriggers = new string[] { "Attack" };
        private bool attacking;

        private float stateTimer;
        private float weaponTimer;
        private float decisionTimer;
        private float recentDamageTimer;
        private CoverPoint reservedCover;
        private NpcPatrol patrol;
        private int strafeSign;

        private bool WasDamagedRecently => recentDamageTimer > 0f;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            perception = GetComponent<RobotPerception>();
            health = GetComponent<HealthSystem>();
            
            // AMBIL KOMPONEN ANIMATOR DI SINI
            anim = GetComponentInChildren<Animator>(); 

            patrol = GetComponent<NpcPatrol>();
            if (patrol == null) patrol = gameObject.AddComponent<NpcPatrol>();

            if (utilityWeights == null)
            {
                Debug.LogError($"{name}: field 'Utility Weights' di RobotFSM belum diisi!", this);
                enabled = false;
                return;
            }
            scorer = new UtilityScorer(utilityWeights);
        }

        private void OnEnable()
        {
            if (health == null) return;
            health.OnDeath += HandleDeath;
            health.OnDamaged += HandleDamaged;
        }

        private void OnDisable()
        {
            if (health == null) return;
            health.OnDeath -= HandleDeath;
            health.OnDamaged -= HandleDamaged;
        }

        private void Update()
        {
            if (CurrentState == RobotState.Dead) return;

            stateTimer += Time.deltaTime;
            weaponTimer -= Time.deltaTime;
            decisionTimer -= Time.deltaTime;
            recentDamageTimer -= Time.deltaTime;

            // --- LOGIKA ANIMASI (BARU) ---
            if (anim != null && agent != null)
{
    // Jika robot sedang bergerak, set Speed ke 1. Jika diam, set ke 0.
    float currentSpeed = (agent.velocity.magnitude > 0.1f) ? 1f : 0f;
    anim.SetFloat("Speed", currentSpeed);
}       
            // -----------------------------

            // Saat bertarung dan melihat Player, arah badan diatur FaceTarget(), bukan oleh agent
            agent.updateRotation = !(CurrentState == RobotState.Combat && perception.CanSeePlayer);

            if (stationarySniper)
            {
                agent.isStopped = true;
                agent.updateRotation = false;

                if (Time.frameCount % 120 == 0)
                {
                    Debug.Log(
                        $"SNIPER DEBUG | " +
                        $"Player ditemukan: {perception.PlayerTransform != null} | " +
                        $"Bisa melihat: {perception.CanSeePlayer} | " +
                        $"Zone terpasang: {sniperZone != null} | " +
                        $"Player dalam zona: " +
                        $"{(sniperZone != null && perception.PlayerTransform != null && sniperZone.IsPlayerInside(perception.PlayerTransform))}"
                    );
                }

                if (agent.isOnNavMesh && agent.hasPath)
                    agent.ResetPath();

                if (sniperZone != null &&
                    perception.CanSeePlayer &&
                    perception.PlayerTransform != null &&
                    sniperZone.IsPlayerInside(perception.PlayerTransform))
                {
                    FaceTarget(perception.PlayerTransform.position);
                    TryFireWeapon();
                }

                return;
            }

            switch (CurrentState)
            {
                case RobotState.Idle:    HandleIdle();    break;
                case RobotState.Alert:   HandleAlert();   break;
                case RobotState.Combat:  HandleCombat();  break;
                case RobotState.Retreat: HandleRetreat(); break;
            }
        }

        private void ChangeState(RobotState newState)
        {
            if (CurrentState == newState) return;
            CurrentState = newState;
            stateTimer = 0f;
            decisionTimer = 0f; 
            if (newState != RobotState.Retreat && newState != RobotState.Combat) ReleaseCover();
            if (newState == RobotState.Idle) patrol.Begin();
        }

        private void HandleIdle()
        {
            if (perception.CanSeePlayer)
            {
                ChangeState(RobotState.Alert);
                return;
            }

            patrol.Tick(agent);   // berpatroli, tidak berdiri diam
        }

        private void HandleAlert()
        {
            agent.isStopped = true;
            if (!perception.CanSeePlayer && !perception.HasMemoryOfPlayer)
            {
                ChangeState(RobotState.Idle);
                return;
            }
            if (perception.CanSeePlayer) FaceTarget(perception.PlayerTransform.position);
            if (stateTimer >= alertDelay) ChangeState(RobotState.Combat);
        }

        private void HandleCombat()
        {
            agent.isStopped = false;
            if (!perception.CanSeePlayer && !perception.HasMemoryOfPlayer)
            {
                ChangeState(RobotState.Idle);
                return;
            }

            if (health.HPPercent <= utilityWeights.retreatHPThreshold)
            {
                ChangeState(RobotState.Retreat);
                return;
            }

            Vector3 targetPos = GetThreatPosition();
            float distance = Vector3.Distance(transform.position, targetPos);

            if (decisionTimer <= 0f)
            {
                decisionTimer = decisionInterval;
                bool coverAvailable = CoverPointManager.Instance != null &&
                    CoverPointManager.Instance.FindNearestAvailableCover(transform.position, targetPos, transform) != null;
                bool weaponReady = utilityWeights.isMelee || weaponTimer <= 0f;

                LastUtilityScores = scorer.GetScores(distance, health.HPPercent, WasDamagedRecently, coverAvailable, weaponReady);
                LastChosenAction = scorer.ChooseBestAction(distance, health.HPPercent, WasDamagedRecently, coverAvailable, weaponReady);
                ExecuteAction(LastChosenAction, targetPos, distance);
            }
            if (perception.CanSeePlayer) FaceTarget(targetPos);
        }

        private void ExecuteAction(UtilityAction action, Vector3 targetPos, float distance)
        {
            bool canShoot = perception.CanSeePlayer && distance <= utilityWeights.attackRange;

            // Attacker jarak dekat: kejar lalu pukul/tendang
            if (utilityWeights.isMelee && action == UtilityAction.Attack)
            {
                HandleMeleeAttack(targetPos, distance);
                return;
            }

            switch (action)
            {
                case UtilityAction.Attack:
                    if (canShoot)
                    {
                        if (utilityWeights.keepMovingWhileAttacking) StrafeAround(targetPos, distance);
                        else agent.ResetPath();
                        TryFireWeapon();
                    }
                    else { NavUtil.TryMoveTo(agent, targetPos); }
                    break;
                case UtilityAction.MaintainDistance:
                    Vector3 away = (transform.position - targetPos).normalized;
                    NavUtil.TryMoveTo(agent, transform.position + away * 4f);
                    if (canShoot) TryFireWeapon();
                    break;
                case UtilityAction.TakeCover:
                    CoverPoint cp = CoverPointManager.Instance.FindNearestAvailableCover(transform.position, targetPos, transform);
                    if (cp != null) { ReserveCover(cp); NavUtil.TryMoveTo(agent, cp.transform.position); }
                    break;
                case UtilityAction.Retreat:
                    ChangeState(RobotState.Retreat);
                    break;
            }
        }

        // Bergerak melingkar di sekitar Player supaya robot bukan sasaran diam
        private void StrafeAround(Vector3 targetPos, float distance)
        {
            Vector3 toTarget = targetPos - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.01f) return;
            toTarget.Normalize();

            // Sesekali ganti arah geser (kiri/kanan)
            if (strafeSign == 0 || Random.value < 0.25f) strafeSign = Random.value < 0.5f ? -1 : 1;

            Vector3 side = Vector3.Cross(Vector3.up, toTarget) * strafeSign;

            // Dekati atau jauhi sedikit supaya jaraknya mendekati jarak ideal
            float forward = 0f;
            if (distance > utilityWeights.idealCombatDistance * 1.3f) forward = 1f;
            else if (distance < utilityWeights.idealCombatDistance * 0.7f) forward = -1f;

            Vector3 destination = transform.position + side * utilityWeights.strafeDistance + toTarget * forward * 2f;
            NavUtil.TryMoveTo(agent, destination);
        }

                // ---------- SERANGAN JARAK DEKAT ----------
        private void HandleMeleeAttack(Vector3 targetPos, float distance)
        {
            bool inReach = perception.CanSeePlayer && distance <= utilityWeights.meleeRange;

            if (!inReach)
            {
                NavUtil.TryMoveTo(agent, targetPos);   // belum dekat: kejar Player
                return;
            }

            agent.ResetPath();                          // sudah dekat: berhenti

            if (weaponTimer > 0f || attacking) return;

            weaponTimer = weaponCooldown;
            StartCoroutine(MeleeStrike());
        }

        private System.Collections.IEnumerator MeleeStrike()
        {
            attacking = true;

            if (anim != null && attackTriggers != null && attackTriggers.Length > 0)
            {
                string trigger = attackTriggers[Random.Range(0, attackTriggers.Length)];
                if (HasTrigger(trigger)) anim.SetTrigger(trigger);
            }

            yield return new WaitForSeconds(meleeHitDelay);

            // Beri damage hanya kalau Player masih dalam jangkauan
            if (CurrentState == RobotState.Combat && perception.PlayerTransform != null)
            {
                HealthSystem target = perception.PlayerTransform.GetComponent<HealthSystem>();
                float d = Vector3.Distance(transform.position, perception.PlayerTransform.position);

                if (target != null && !target.IsDead && target.Team != health.Team &&
                    d <= utilityWeights.meleeRange * 1.3f)
                {
                    target.TakeDamage(damage);
                }
            }

            attacking = false;
        }

        private bool HasTrigger(string triggerName)
        {
            if (anim == null) return false;
            foreach (AnimatorControllerParameter p in anim.parameters)
            {
                if (p.name == triggerName && p.type == AnimatorControllerParameterType.Trigger) return true;
            }
            return false;
        }
        private void TryFireWeapon()
        {
            if (weaponTimer > 0f || projectilePrefab == null || firePoint == null) return;
            if (!perception.CanSeePlayer) return;

            // --- LOGIKA ANIMASI SERANGAN (BARU) ---
            if (anim != null) anim.SetTrigger("Attack");
            // ---------------------------------------

            Vector3 direction = (perception.PlayerAimPosition - firePoint.position).normalized;
            GameObject bullet = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(direction));

            Projectile projectile = bullet.GetComponent<Projectile>();
            if (projectile != null) projectile.Initialize(damage, health.Team);

            weaponTimer = weaponCooldown;
            if (utilityWeights.hideAfterShooting) recentDamageTimer = recentDamageWindow;
        }

        private void HandleRetreat()
        {
            agent.isStopped = false;
            if (health.HPPercent > utilityWeights.retreatHPThreshold * 1.5f)
            {
                ChangeState(perception.HasMemoryOfPlayer ? RobotState.Combat : RobotState.Idle);
                return;
            }
            if (decisionTimer > 0f) return;
            decisionTimer = decisionInterval;
            Vector3 threat = GetThreatPosition();

            CoverPoint cp = CoverPointManager.Instance != null
                ? CoverPointManager.Instance.FindNearestAvailableCover(transform.position, threat, transform) : null;

            if (cp != null) { ReserveCover(cp); NavUtil.TryMoveTo(agent, cp.transform.position); }
            else { Vector3 away = (transform.position - threat).normalized; NavUtil.TryMoveTo(agent, transform.position + away * retreatDistance); }
        }

        private Vector3 GetThreatPosition() => perception.CanSeePlayer ? perception.PlayerTransform.position : perception.LastKnownPlayerPosition;

        private void FaceTarget(Vector3 worldPos)
        {
            Vector3 flat = new Vector3(worldPos.x, transform.position.y, worldPos.z);
            if ((flat - transform.position).sqrMagnitude > 0.01f) transform.LookAt(flat);
        }

        private void ReserveCover(CoverPoint cp)
        {
            if (reservedCover != null && reservedCover != cp) reservedCover.Release(transform);
            cp.Reserve(transform);
            reservedCover = cp;
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
            ChangeState(RobotState.Dead);
            ReleaseCover();
            agent.isStopped = true;
            foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;
            enabled = false;
            Destroy(gameObject, 2f);
        }

        public string GetDebugText()
        {
            var s = LastUtilityScores;
            return $"{CurrentState}  [{LastChosenAction}]\nA:{s.attack:F2} D:{s.distance:F2} C:{s.cover:F2} R:{s.retreat:F2}\nHP:{health.CurrentHP}/{health.MaxHP}";
        }
    }
}