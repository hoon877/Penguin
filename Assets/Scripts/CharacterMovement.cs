using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class CharacterMovement : MonoBehaviour
{
    [SerializeField] GameObject DeadPanelPrefab;
    [SerializeField] Transform target; // ��� ĳ������ Transform
    [SerializeField] float moveSpeed = 3.0f;
    [SerializeField] float killRange = 1.0f;
    Vector2 moveDir;
    Rigidbody2D rb;
    Animator animator;
    float vertical;
    float horizontal;
    bool isDead = false;
    float seconds = 1f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        moveDir = Vector2.zero;
    }

    private void FixedUpdate()
    {
        if (!isDead)
        {
            Move();
        }
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
        if (!isDead)
        {
            if (horizontal != 0 || vertical != 0)
            {
                animator.SetBool("Walk", true);
            }
            else
            {
                animator.SetBool("Walk", false);
            }
        }
        else
        {
            animator.SetBool("Walk", false); // ?? �׾��� �� Walk false ����
        }

        Kill();
    }

    private void Kill()
    {
        if (Input.GetMouseButtonDown(0) && !isDead && IsInKillRange())
        {
            // ��� ID ��������
            var identifier = target.GetComponent<NetworkPlayerIdentifier>();
            if (identifier == null) return;

            string targetId = identifier.playerId;

            // ������ ų ��û
            NetworkManager.Instance.socket.Emit("kill", new { targetId });

            Debug.Log("?? ų ��û ����: " + targetId);

            NetworkManager.Instance.socket.On("killed", (data) =>
            {
                JObject json = JObject.Parse(data.ToString());

                string victimId = json["victimId"]?.ToString();
                string myId = NetworkManager.Instance.socket.Id;

                if (victimId == myId)
                {
                    Debug.Log("? ���� �׾���");
                    isDead = true;
                    GameObject panelInstance = Instantiate(DeadPanelPrefab, Vector3.zero, Quaternion.identity);
                    Destroy(panelInstance, seconds);
                    StartCoroutine(ReviveAfterDelay());
                }
                else
                {
                    Debug.Log("? �ٸ� ����� �׾���: " + victimId);
                    //if (otherPlayers.ContainsKey(victimId))
                    //{
                    //    otherPlayers[victimId].SetActive(false); // or death animation
                    //}
                }
            });
        }
    }

    private bool IsInKillRange()
    {
        float distance = Vector2.Distance(transform.position, target.position);
        return distance <= killRange;
    }

    IEnumerator ReviveAfterDelay()
    {
        yield return new WaitForSeconds(seconds);
        isDead = false;
    }
}
