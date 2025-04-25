using UnityEngine;

public class CharacterAnimatorController : MonoBehaviour
{
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void SetDead()
    {
        animator.SetBool("Dead", true);
        animator.SetBool("Walk", false);
        Debug.Log($"{name}: Dead 애니메이션 실행됨");
    }

    public void SetWalk(bool isWalking)
    {
        animator.SetBool("Walk", isWalking);
    }
}