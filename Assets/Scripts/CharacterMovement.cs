using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterMovement : MonoBehaviour
{
    [SerializeField] float moveSpeed = 3.0f;
    Vector2 moveDir;
    Rigidbody2D rb;
    Animator animator;
    float vertical;
    float horizontal;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        moveDir = Vector2.zero;
    }

    private void FixedUpdate()
    {
        Move();
    }

    void Move()
    {
        horizontal = Input.GetAxisRaw("Horizontal");
        vertical = Input.GetAxisRaw("Vertical");

        moveDir.x = rb.position.x + (horizontal * moveSpeed * Time.deltaTime);
        moveDir.y = rb.position.y + (vertical *moveSpeed * Time.deltaTime);

        rb.MovePosition(moveDir);
    }

    private void Update()
    {
        if(horizontal != 0 || vertical != 0)
        {
            animator.SetBool("Walk", true);
        }
        else if(horizontal == 0 && vertical == 0)
        {
            animator.SetBool("Walk", false);
        }

        if(Input.GetKeyDown(KeyCode.Space))
        {
            
        }
    }
}
