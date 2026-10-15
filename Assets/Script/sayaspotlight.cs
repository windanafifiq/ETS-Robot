using UnityEngine;

public class SpotlightAim : MonoBehaviour
{
    public Transform target;
    public float rotationSpeed = 2f;

    void LateUpdate()
    {
        if (target == null) return;

        Quaternion targetRotation = Quaternion.LookRotation(
            target.position - transform.position
        );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }
}