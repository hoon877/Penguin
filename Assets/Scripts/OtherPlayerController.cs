using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OtherPlayerController : MonoBehaviour
{
    [SerializeField] float moveSpeed = 3.0f;
    Animator animator;
    private bool isWalking;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }
    
    private void Update()
    {
        if(isWalking)
        {
            animator.SetBool("Walk", true);
        }
        else if(!isWalking)
        {
            animator.SetBool("Walk", false);
        }

        if(Input.GetKeyDown(KeyCode.Space))
        {
            
        }
    }
}
