
using UnityEngine;

public class RobotPunchController : MonoBehaviour
{
    [Header("Animation")]
    public Animator animator;

    [Header("Punch Settings")]
    public float punchCooldown = 0.7f;

    private float nextPunchTime;

    void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F) &&
            Time.time >= nextPunchTime)
        {
            Punch();
        }
    }

    void Punch()
    {
        if (animator == null)
        {
            Debug.LogWarning("Animator belum terpasang!");
            return;
        }

        animator.SetTrigger("Punch");

        nextPunchTime = Time.time + punchCooldown;

        Debug.Log("Robot melakukan pukulan kanan!");
    }
}