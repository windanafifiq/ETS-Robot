
using UnityEngine;

public class SniperZone : MonoBehaviour
{
    [Header("Zone Settings")]
    [SerializeField] private LayerMask playerLayer;

    private Collider[] playersInZone = new Collider[10];
    private BoxCollider zoneCollider;

    private void Awake()
    {
        zoneCollider = GetComponent<BoxCollider>();
    }

    public bool IsPlayerInside(Transform player)
    {
        if (player == null || zoneCollider == null)
            return false;

        // Periksa apakah posisi player berada di dalam zona
        Vector3 localPoint = transform.InverseTransformPoint(player.position);

        Vector3 center = zoneCollider.center;
        Vector3 halfSize = zoneCollider.size * 0.5f;

        return Mathf.Abs(localPoint.x - center.x) <= halfSize.x &&
               Mathf.Abs(localPoint.y - center.y) <= halfSize.y &&
               Mathf.Abs(localPoint.z - center.z) <= halfSize.z;
    }
}