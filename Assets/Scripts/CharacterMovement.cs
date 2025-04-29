using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
    
    private Button killButton;
    private TMP_Text killButtonText;
    
    private float killCooldown = 30f;
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
            killButton = GameObject.Find("KillButton")?.GetComponent<Button>();
            killButtonText = GameObject.Find("KillButtonText")?.GetComponent<TMP_Text>();

            if (killButton != null)
            {
                killButton.onClick.AddListener(OnKillButtonClicked);
                killButton.interactable = false; // 처음에는 비활성화
            }

            StartCoroutine(KillCooldownRoutine());
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

        if (Input.GetKeyDown(KeyCode.Space))
        {
            SetDead();
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
        
        if (killButton != null)
        {
            killButton.interactable = false;
        }
        if (killButtonText != null)
        {
            killButtonText.text = null; // ✅ 죽으면 즉시 텍스트 비우기
        }
    }
    
    IEnumerator KillCooldownRoutine()
    {
        float remainingTime = killCooldown;

        while (remainingTime > 0f)
        {
            if (isDead) yield break;
            
            if (killButtonText != null)
            {
                killButtonText.text = $"{Mathf.CeilToInt(remainingTime)}초";
            }

            remainingTime -= Time.deltaTime;
            yield return null;
        }
        
        if (isDead) yield break;

        if (killButton != null)
        {
            killButton.interactable = true;
        }
        if (killButtonText != null)
        {
            killButtonText.text = null;
        }

        canKill = true;
    }
    
    private void OnKillButtonClicked()
    {
        if (!canKill) return; // 아직 킬 불가

        GameObject closest = FindClosestKillableTarget();
        if (closest == null) return;

        var identifier = closest.GetComponent<NetworkPlayerIdentifier>();
        if (identifier == null || string.IsNullOrEmpty(identifier.playerId)) return;

        isKill = true;
        StartCoroutine(SetKillCooldown());

        string targetId = identifier.playerId;
        NetworkManager.Instance.socket.Emit("kill", new { targetId });
        
        StartCoroutine(RestartKillCooldown());
    }
    
    IEnumerator RestartKillCooldown()
    {
        // 즉시 버튼 비활성화
        if (killButton != null)
        {
            killButton.interactable = false;
        }
        if (killButtonText != null)
        {
            killButtonText.text = $"{Mathf.CeilToInt(killCooldown)}초";
        }

        canKill = false; // 다시 쿨다운 시작
        float remainingTime = killCooldown;

        while (remainingTime > 0f)
        {
            if (isDead) yield break;
            
            if (killButtonText != null)
            {
                killButtonText.text = $"{Mathf.CeilToInt(remainingTime)}초";
            }

            remainingTime -= Time.deltaTime;
            yield return null;
        }

        if (isDead) yield break;
        
        if (killButton != null)
        {
            killButton.interactable = true;
        }
        if (killButtonText != null)
        {
            killButtonText.text = "KILL";
        }

        canKill = true;
    }
}
