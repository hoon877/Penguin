using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class CharacterMovement : MonoBehaviour
{
    [SerializeField] GameObject DeadPanelPrefab;
    [SerializeField] Transform target; // 상대 캐릭터의 Transform
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
            animator.SetBool("Walk", false); // ?? 죽었을 땐 Walk false 고정
        }

        Kill();
    }

    private void Kill()
    {
        if (Input.GetMouseButtonDown(0) && !isDead && IsInKillRange())
        {
            // 상대 ID 가져오기
            var identifier = target.GetComponent<NetworkPlayerIdentifier>();
            if (identifier == null) return;

            string targetId = identifier.playerId;

            // 서버에 킬 요청
            NetworkManager.Instance.socket.Emit("kill", new { targetId });

            Debug.Log("?? 킬 요청 전송: " + targetId);

            NetworkManager.Instance.socket.On("killed", (data) =>
            {
                JObject json = JObject.Parse(data.ToString());

                string victimId = json["victimId"]?.ToString();
                string myId = NetworkManager.Instance.socket.Id;

                if (victimId == myId)
                {
                    Debug.Log("? 내가 죽었음");
                    isDead = true;
                    GameObject panelInstance = Instantiate(DeadPanelPrefab, Vector3.zero, Quaternion.identity);
                    Destroy(panelInstance, seconds);
                    StartCoroutine(ReviveAfterDelay());
                }
                else
                {
                    Debug.Log("? 다른 사람이 죽었음: " + victimId);
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
