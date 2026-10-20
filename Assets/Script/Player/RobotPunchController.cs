
using UnityEngine;

public class RobotPunchController : MonoBehaviour
{
    public Animator animator;
    public float punchCooldown = 0.6f;

    private float nextPunchTime;
    private bool punchRight = true;

    void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F)
            && Time.time >= nextPunchTime)
        {
            Punch();
        }
    }

    void Punch()
    {
        if (animator == null) return;

        if (punchRight)
        {
            animator.SetTrigger("Punch");
            Debug.Log("Robot memukul KANAN!");
        }
        else
        {
            animator.SetTrigger("PunchLeft");
            Debug.Log("Robot memukul KIRI!");
        }

        punchRight = !punchRight;
        nextPunchTime = Time.time + punchCooldown;
    }
}
