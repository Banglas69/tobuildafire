using UnityEngine;

public class MoveXYToAnimator : MonoBehaviour
{
    public Animator animator;
    public float damp = 0.00f;

    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        animator.SetFloat("MoveX", x, damp, Time.deltaTime);
        animator.SetFloat("MoveY", y, damp, Time.deltaTime);
    }
}
