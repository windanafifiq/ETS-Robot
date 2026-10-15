
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerTestMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 8f;
    public float groundCheckDistance = 0.35f;
    public float maxSlopeAngle = 40f;

    private Rigidbody rb;
    private CapsuleCollider capsule;
    private Vector3 movement;
    private Vector3 groundNormal = Vector3.up;
    private bool isGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();

        rb.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        movement = new Vector3(horizontal, 0f, vertical).normalized;
    }

    void FixedUpdate()
    {
        CheckGround();

        if (isGrounded)
        {
            Vector3 slopeMovement =
                Vector3.ProjectOnPlane(movement, groundNormal).normalized;

            Vector3 velocity = slopeMovement * moveSpeed;

            // Gunakan Rigidbody velocity agar collision tetap diproses.
            rb.linearVelocity = new Vector3(
                velocity.x,
                velocity.y,
                velocity.z
            );
        }
        else
        {
            // Tetap izinkan gerakan horizontal saat tidak grounded.
            Vector3 velocity = rb.linearVelocity;

            rb.linearVelocity = new Vector3(
                movement.x * moveSpeed,
                velocity.y,
                movement.z * moveSpeed
            );
        }
    }

    void CheckGround()
    {
        Vector3 origin = transform.position +
            Vector3.up * 0.1f;

        float rayDistance =
            capsule.height * 0.5f + groundCheckDistance;

        if (Physics.Raycast(
            origin,
            Vector3.down,
            out RaycastHit hit,
            rayDistance,
            ~0,
            QueryTriggerInteraction.Ignore))
        {
            float angle = Vector3.Angle(
                hit.normal,
                Vector3.up
            );

            isGrounded = angle <= maxSlopeAngle;
            groundNormal = hit.normal;
        }
        else
        {
            isGrounded = false;
            groundNormal = Vector3.up;
        }
    }
}
