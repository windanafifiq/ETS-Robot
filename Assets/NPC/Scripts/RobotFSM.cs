using UnityEngine;
using UnityEngine.AI;
using RobotArena; 
using RobotArena.Environment;
using RobotArena.DebugTools;

namespace RobotArena.Enemy
{
    public enum RobotState { Idle, Alert, Combat, Retreat, Dead }
    
    public enum SniperState { Idle, Alert, Combat, Tracking, Dead }

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

        [Header("Sniper FSM & Utility AI")]
        [SerializeField] private float sniperTrackingDuration = 4f;

        // Sistem pengambilan keputusan khusus Sniper
        private SniperUtilityScorer sniperScorer;

        // Menyimpan hasil keputusan Utility AI untuk debugging
        private SniperAction currentSniperAction = SniperAction.DefensiveHold;
        private SniperUtilityScorer.Scores sniperScores;

        public SniperState CurrentSniperState { get; private set; }
            = SniperState.Idle;

        private float sniperStateTimer;

        private void ChangeSniperState(SniperState newState)
        {
            if (CurrentSniperState == newState)
                return;

            CurrentSniperState = newState;
            sniperStateTimer = 0f;
        }

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
        private Animator anim; 
        
        // --- VARIABEL AUDIO BARU ---
        private AudioSource audioSource;
        // ---------------------------

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
            
            anim = GetComponentInChildren<Animator>(); 
            
            // --- AMBIL KOMPONEN AUDIO BARU ---
            audioSource = GetComponent<AudioSource>(); 
            // ---------------------------------

            patrol = GetComponent<NpcPatrol>();
            if (patrol == null) patrol = gameObject.AddComponent<NpcPatrol>();

            if (utilityWeights == null)
            {
                Debug.LogError($"{name}: field 'Utility Weights' di RobotFSM belum diisi!", this);
                enabled = false;
                return;
            }
            scorer = new UtilityScorer(utilityWeights);
            sniperScorer = new SniperUtilityScorer();
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

            if (anim != null && agent != null)
            {
                float currentSpeed =
                    agent.velocity.magnitude > 0.1f ? 1f : 0f;

                if (HasFloatParameter("Speed"))
                    anim.SetFloat("Speed", currentSpeed);
            }    

            agent.updateRotation = !(CurrentState == RobotState.Combat && perception.CanSeePlayer);

            if (stationarySniper)
            {
                HandleStationarySniper();
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

        // Sniper hanya memburu target yang terlihat di zona, tetapi boleh
        // mengingat posisi terakhir di dalam zona saat LOS terputus.
        private bool SniperCanSeeTarget()
        {
            return sniperZone != null &&
                   sniperZone.isActiveAndEnabled &&
                   perception.CanSeePlayer &&
                   perception.PlayerTransform != null &&
                   sniperZone.IsPlayerInside(perception.PlayerTransform);
        }

        private bool IsRememberedPositionInsideSniperZone(Vector3 worldPoint)
        {
            BoxCollider box = sniperZone.GetComponent<BoxCollider>();
            if (box == null || !box.enabled) return false;
            Vector3 local = box.transform.InverseTransformPoint(worldPoint) - box.center;
            Vector3 half = box.size * 0.5f;
            return Mathf.Abs(local.x) <= half.x &&
                   Mathf.Abs(local.y) <= half.y &&
                   Mathf.Abs(local.z) <= half.z;
        }

        private bool SniperRemembersTarget()
        {
            return sniperZone != null &&
                   sniperZone.isActiveAndEnabled &&
                   perception.HasMemoryOfPlayer &&
                   perception.TimeSinceLastSeen <= sniperTrackingDuration &&
                   IsRememberedPositionInsideSniperZone(perception.LastKnownPlayerPosition);
        }

        private void UpdateSniperState(bool visible, bool remembered)
        {
            sniperStateTimer += Time.deltaTime;

            switch (CurrentSniperState)
            {
                case SniperState.Idle:
                    if (visible) ChangeSniperState(SniperState.Alert);
                    break;

                case SniperState.Alert:
                    if (!visible)
                        ChangeSniperState(remembered ? SniperState.Tracking : SniperState.Idle);
                    else if (sniperStateTimer >= alertDelay)
                        ChangeSniperState(SniperState.Combat);
                    break;

                case SniperState.Combat:
                    if (!visible)
                        ChangeSniperState(remembered ? SniperState.Tracking : SniperState.Idle);
                    break;

                case SniperState.Tracking:
                    if (visible)
                        ChangeSniperState(SniperState.Alert);
                    else if (!remembered)
                        ChangeSniperState(SniperState.Idle);
                    break;

                case SniperState.Dead:
                    break;
            }
        }

        private void HandleStationarySniper()
        {
            // Jangan berpindah dari tower.
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                if (agent.hasPath) agent.ResetPath();
            }
            agent.updateRotation = false;

            bool visible = SniperCanSeeTarget();
            bool remembered = SniperRemembersTarget();
            UpdateSniperState(visible, remembered);

            // Skor dihitung dari kondisi nyata; hanya Combat yang memakai
            // pemilihan aksi Utility untuk menentukan tembak vs bertahan.
            sniperScores = sniperScorer.GetScores(
                visible,
                visible || remembered,
                remembered,
                weaponTimer <= 0f,
                health.HPPercent
            );

            switch (CurrentSniperState)
            {
                case SniperState.Idle:
                    currentSniperAction = SniperAction.DefensiveHold;
                    break;

                case SniperState.Alert:
                    currentSniperAction = SniperAction.DefensiveHold;
                    if (visible) FaceTarget(perception.PlayerTransform.position);
                    break;

                case SniperState.Tracking:
                    currentSniperAction = remembered
                        ? SniperAction.TrackTarget
                        : SniperAction.DefensiveHold;
                    if (remembered) FaceTarget(perception.LastKnownPlayerPosition);
                    break;

                case SniperState.Combat:
                    // Tetap arahkan badan ke Player saat cooldown.
                    if (visible) FaceTarget(perception.PlayerTransform.position);
                    currentSniperAction = sniperScorer.ChooseBestAction(sniperScores);
                    if (currentSniperAction == SniperAction.Shoot && visible)
                        TryFireWeapon();
                    break;

                case SniperState.Dead:
                    currentSniperAction = SniperAction.DefensiveHold;
                    break;
            }
        }

        private void HandleIdle()
        {
            if (perception.CanSeePlayer)
            {
                ChangeState(RobotState.Alert);
                return;
            }
            patrol.Tick(agent);   
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

        private void StrafeAround(Vector3 targetPos, float distance)
        {
            Vector3 toTarget = targetPos - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.01f) return;
            toTarget.Normalize();

            if (strafeSign == 0 || Random.value < 0.25f) strafeSign = Random.value < 0.5f ? -1 : 1;

            Vector3 side = Vector3.Cross(Vector3.up, toTarget) * strafeSign;

            float forward = 0f;
            if (distance > utilityWeights.idealCombatDistance * 1.3f) forward = 1f;
            else if (distance < utilityWeights.idealCombatDistance * 0.7f) forward = -1f;

            Vector3 destination = transform.position + side * utilityWeights.strafeDistance + toTarget * forward * 2f;
            NavUtil.TryMoveTo(agent, destination);
        }

        private void HandleMeleeAttack(Vector3 targetPos, float distance)
        {
            bool inReach = perception.CanSeePlayer && distance <= utilityWeights.meleeRange;

            if (!inReach)
            {
                NavUtil.TryMoveTo(agent, targetPos);   
                return;
            }

            agent.ResetPath();                          

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

        private bool HasFloatParameter(string parameterName)
        {
            if (anim == null || anim.runtimeAnimatorController == null) return false;
            foreach (AnimatorControllerParameter parameter in anim.parameters)
                if (parameter.name == parameterName && parameter.type == AnimatorControllerParameterType.Float)
                    return true;
            return false;
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

            // --- LOGIKA AUDIO BARU ---
            if (audioSource != null && audioSource.clip != null) 
            {
                audioSource.Play(); 
            }
            // -------------------------

            // Audio tembakan dari main
            if (audioSource != null && audioSource.clip != null)
            {
                audioSource.Play();
            }

            // Animasi serangan
            if (anim != null && HasTrigger("Attack"))
            {
                anim.SetTrigger("Attack");
            }

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

            if (stationarySniper)
                ChangeSniperState(SniperState.Dead);
                
            ChangeState(RobotState.Dead);
            ReleaseCover();
            agent.isStopped = true;
            foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;
            enabled = false;
            Destroy(gameObject, 2f);
        }

        public string GetDebugText()
        {
            if (stationarySniper)
            {
                return
                    $"SNIPER | {CurrentSniperState}\n" +
                    $"Action: {currentSniperAction}\n" +
                    $"Shoot:{sniperScores.shoot:F2} " +
                    $"Track:{sniperScores.track:F2} " +
                    $"Defend:{sniperScores.defensive:F2}\n" +
                    $"HP:{health.CurrentHP}/{health.MaxHP}";
            }

            var s = LastUtilityScores;
            return $"{CurrentState} [{LastChosenAction}]\n" +
                   $"A:{s.attack:F2} D:{s.distance:F2} C:{s.cover:F2} R:{s.retreat:F2}\n" +
                   $"HP:{health.CurrentHP}/{health.MaxHP}";
        }
    }
}