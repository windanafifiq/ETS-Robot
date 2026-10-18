using UnityEngine;
using RobotArena; 

namespace RobotArena.Enemy
{
    public class RobotPerception : MonoBehaviour
    {
        [Header("Perception")]
        [SerializeField] private float detectionRadius = 14f;
        [SerializeField] private LayerMask playerLayer;
        [SerializeField] private LayerMask obstacleLayer;
        [SerializeField] private Transform eyePoint;
        [SerializeField] private float targetHeightOffset = 1f;
        [SerializeField] private float scanInterval = 0.2f;

        [Header("Memory")]
        [SerializeField] private float memoryDuration = 4f;

        public float DetectionRadius => detectionRadius;
        public bool CanSeePlayer { get; private set; }
        public Transform PlayerTransform { get; private set; }
        public Vector3 LastKnownPlayerPosition { get; private set; }
        public float DistanceToPlayer { get; private set; } = Mathf.Infinity;
        public float TimeSinceLastSeen { get; private set; } = Mathf.Infinity;

        public bool HasMemoryOfPlayer => PlayerTransform != null && TimeSinceLastSeen <= memoryDuration;
        public Vector3 PlayerAimPosition => PlayerTransform != null ? PlayerTransform.position + Vector3.up * targetHeightOffset : LastKnownPlayerPosition;

        private float scanTimer;
        private HealthSystem myHealth;

        private void Awake()
        {
            myHealth = GetComponent<HealthSystem>();
        }

        private void Update()
        {
            scanTimer -= Time.deltaTime;
            TimeSinceLastSeen += Time.deltaTime;

            if (scanTimer <= 0f) { Scan(); scanTimer = scanInterval; }
        }

        private void Scan()
        {
            CanSeePlayer = false;
            Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, playerLayer);
            if (hits.Length == 0) { DistanceToPlayer = Mathf.Infinity; return; }

            HealthSystem hs = hits[0].GetComponentInParent<HealthSystem>();
            
            if (hs == null || hs.IsDead || (myHealth != null && hs.Team == myHealth.Team))
            {
                DistanceToPlayer = Mathf.Infinity;
                return;
            }

            Transform target = hs.transform;
            DistanceToPlayer = Vector3.Distance(transform.position, target.position);

            Vector3 origin = eyePoint != null ? eyePoint.position : transform.position + Vector3.up;
            Vector3 targetPoint = target.position + Vector3.up * targetHeightOffset;
            Vector3 direction = targetPoint - origin;
            float distance = direction.magnitude;

            if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, distance, obstacleLayer | playerLayer))
            {
                bool hitIsPlayer = ((1 << hit.collider.gameObject.layer) & playerLayer) != 0;
                if (hitIsPlayer)
                {
                    CanSeePlayer = true;
                    PlayerTransform = target;
                    LastKnownPlayerPosition = target.position;
                    TimeSinceLastSeen = 0f;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }
}