using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CharacterMovement : MonoBehaviour
{
    [SerializeField] GameObject DeadPanelPrefab;
    [SerializeField] float moveSpeed = 3.0f;
    [SerializeField] float killRange = 10.0f;

    private string myId;
    private Vector2 moveDir;
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    
    float vertical;
    float horizontal;
    bool isDead = false;
    bool isKill = false;
    float seconds = 1f;
    private bool canKill = false; // 처음에는 Kill 불가
    private bool isKillableScene = false;
    private string currentSceneName;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        moveDir = Vector2.zero;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        currentSceneName = SceneManager.GetActiveScene().name;
        isKillableScene = currentSceneName == "Game";
        if (isKillableScene)
        {
            StartCoroutine(EnableKillAfterDelay());
        }
        
        myId = NetworkManager.Instance.socket.Id;
        
        // 플레이어들 가져오기
        NetworkManager.Instance.socket.Emit("getPlayers");
        NetworkManager.Instance.socket.On("playersInRoom", (data) =>
        {
            JArray arr = JArray.Parse(data.ToString());
            WaitingRoomController.otherPlayers.Clear();

            foreach (var idToken in arr)
            {
                string id = idToken.ToString();
                if (id != myId && !WaitingRoomController.otherPlayers.ContainsKey(id))
                {
                    // 다른 플레이어 정보 받아올 경우 처리
                }
            }
        });

        // ✅ killed 이벤트는 여기에서 1회만 등록!
        NetworkManager.Instance.socket.On("killed", (data) =>
        {
            try
            {
                JArray arr = JArray.Parse(data.ToString());
                if (arr.Count == 0) return;

                JObject json = (JObject)arr[0];
                string victimId = json["victimId"]?.ToString();
                string killerId = json["killerId"]?.ToString();
                MainThreadDispatcher.Enqueue(() =>
                {
                    if (victimId == myId)
                    {
                        isDead = true;
                        GameObject panelInstance = Instantiate(DeadPanelPrefab, Vector3.zero, Quaternion.identity);
                        Destroy(panelInstance, seconds);
                        StartCoroutine(SetKillCooldown());
                        SetDead();
                    }
                    else
                    {
                        if (WaitingRoomController.otherPlayers.TryGetValue(victimId, out GameObject victimGo))
                        {
                            var animCtrl = victimGo.GetComponent<CharacterAnimatorController>();
                            if (animCtrl != null)
                            {
                                animCtrl.SetDead();
                            }
                            else
                            {
                                Debug.LogWarning("상대방 CharacterMovement 없음");
                            }

                            GameObject panelInstance = Instantiate(DeadPanelPrefab, victimGo.transform.position, Quaternion.identity);
                            Destroy(panelInstance, seconds);
                        }
                    }
                });
            }
            catch (System.Exception ex)
            {
                Debug.LogError("killed 파싱 실패: " + ex.Message);
            }
        });
    }

    private void FixedUpdate()
    {
        if (!isDead && !isKill)
        {
            Move();
        }
    }

    void Move()
    {
        horizontal = Input.GetAxisRaw("Horizontal");
        vertical = Input.GetAxisRaw("Vertical");

        moveDir.x = rb.position.x + (horizontal * moveSpeed * Time.deltaTime);
        moveDir.y = rb.position.y + (vertical * moveSpeed * Time.deltaTime);

        rb.MovePosition(moveDir);
        
        // ✅ 왼쪽/오른쪽 이동 방향에 따라 flip 처리
        if (horizontal < 0)
        {
            spriteRenderer.flipX = true;
        }
        else if (horizontal > 0)
        {
            spriteRenderer.flipX = false;
        }
    }

    private void Update()
    {
        if (!isDead)
        {
            animator.SetBool("Walk", horizontal != 0 || vertical != 0);
        }
        else
        {
            animator.SetBool("Walk", false);
        }

        Kill();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            SetDead();
        }
    }

    private void Kill()
    {
        if (!canKill) return;
        
        if (Input.GetMouseButtonDown(0) && !isDead)
        {
            GameObject closest = FindClosestKillableTarget();
            if (closest == null) return;
            var identifier = closest.GetComponent<NetworkPlayerIdentifier>();
            if (identifier == null || string.IsNullOrEmpty(identifier.playerId)) return;
            // ✅ 킬 시 이동 제한
            isKill = true;
            StartCoroutine(SetKillCooldown());
            string targetId = identifier.playerId;
            NetworkManager.Instance.socket.Emit("kill", new { targetId });
        }
    }

    private GameObject FindClosestKillableTarget()
    {
        float minDist = float.MaxValue;
        GameObject closest = null;

        foreach (var kvp in WaitingRoomController.otherPlayers)
        {
            GameObject other = kvp.Value;
            if (other == null) continue;

            float dist = Vector2.Distance((Vector2)transform.position, (Vector2)other.transform.position);
            if (dist < killRange && dist < minDist)
            {
                minDist = dist;
                closest = other;
            }
        }

        return closest;
    }

    IEnumerator SetKillCooldown()
    {
        yield return new WaitForSeconds(seconds);
        isKill = false;
    }

    public void SetDead()
    {
        isDead = true;
        animator.SetBool("Dead", true);
    }
    
    IEnumerator EnableKillAfterDelay()
    {
        yield return new WaitForSeconds(30f);
        canKill = true;
        Debug.Log("이제 킬이 가능합니다!");
    }
}
