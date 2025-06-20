using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CharacterMovement : MonoBehaviour
{
    public static HashSet<string> DeadPlayerIds = new HashSet<string>();

    [SerializeField] GameObject DeadPanelPrefab;
    [SerializeField] float moveSpeed = 3.0f;
    [SerializeField] float killRange = 4.0f;

    [SerializeField] private float eatRange = 4.0f;
    [SerializeField] private float hungerRecovery = 30f;
    private Button eatButton;

    [SerializeField] private string fishingLayerName = "Fishing";
    [SerializeField] private float fishingCooldown = 60f;

    private int fishingLayer;
    private bool isOnFishingTile = false;
    private bool isFishingCooldown = false;
    private bool isFishing = false;
    private Button fishingButton;
    private TMP_Text fishingCooldownText;
    [SerializeField] private GameObject FishingPanelPrefab;

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

            eatButton = GameObject.Find("EatButton")?.GetComponent<Button>();

            fishingButton = GameObject.Find("FishingButton")?.GetComponent<Button>();
            fishingCooldownText = GameObject.Find("FishingCooldownText")?.GetComponent<TMP_Text>();

            if (fishingButton != null)
            {
                fishingButton.onClick.AddListener(OnFishingButtonClicked);
            }
            fishingLayer = LayerMask.NameToLayer(fishingLayerName);
            if (fishingLayer == -1)
                Debug.LogError("Fishing 레이어가 정의되어 있지 않습니다!");

            if (eatButton != null)
                eatButton.onClick.AddListener(OnEatButtonClicked);

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
                    Debug.Log($"[죽음 등록] {victimId} added to DeadPlayerIds");

                    if (victimId == myId)
                    {
                        isDead = true;
                        DeadPlayerIds.Add(myId);
                        if (!string.IsNullOrEmpty(killerId))
                        {
                            GameObject panelInstance = Instantiate(DeadPanelPrefab, Vector3.zero, Quaternion.identity);
                            Destroy(panelInstance, seconds);
                        }
                        StartCoroutine(SetKillCooldown());
                        SetDead();
                    }
                    else
                    {
                        if (WaitingRoomController.otherPlayers.TryGetValue(victimId, out GameObject victimGo))
                        {
                            var animCtrl = victimGo.GetComponent<CharacterAnimatorController>();
                            animCtrl?.SetDead();

                            DeadPlayerIds.Add(victimId);
                        }
                    }
                });
            }
            catch (System.Exception ex)
            {
                Debug.LogError("killed 파싱 실패: " + ex.Message);
            }
        });

        NetworkManager.Instance.socket.On("corpseEaten", (data) =>
        {
            try
            {
                JArray arr = JArray.Parse(data.ToString());
                if (arr.Count == 0) return;

                JObject json = (JObject)arr[0];
                string targetId = json["targetId"]?.ToString();
                if (string.IsNullOrEmpty(targetId)) return;

                MainThreadDispatcher.Enqueue(() =>
                {

                    // 1. 내가 먹힌 시체라면 내 오브젝트 삭제
                    if (targetId == myId)
                    {
                        Debug.Log($"🟥 내가 먹힘. 내 시체 제거됨: {targetId}");
                        CharacterMovement.DeadPlayerIds.Remove(targetId);
                        Destroy(gameObject); // 내 시점에서 스스로 제거
                        
                        return;
                    }

                    // 2. 상대방이면 otherPlayers에서 제거
                    if (WaitingRoomController.otherPlayers.TryGetValue(targetId, out GameObject corpse))
                    {
                        Destroy(corpse);
                        WaitingRoomController.otherPlayers.Remove(targetId);
                        CharacterMovement.DeadPlayerIds.Remove(targetId);
                        Debug.Log($"[동기화] 시체 제거됨: {targetId}");
                    }

                });
            }
            catch (System.Exception ex)
            {
                Debug.LogError("corpseEaten 이벤트 파싱 실패: " + ex.Message);
            }
        });
    }

    private void FixedUpdate()
    {
        if (!isDead && !isKill && !isFishing)
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
        if(isKillableScene)
            fishingButton.interactable = !isDead && isOnFishingTile && !isFishingCooldown;
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
        
        HungerSystem hunger = GetComponent<HungerSystem>();
        hunger?.Kill();

        if (killButton != null)
        {
            killButton.interactable = false;
        }
        if (killButtonText != null)
        {
            killButtonText.text = null; 
        }

        var gsc = FindObjectOfType<GameSceneController>();
        if (gsc != null)
        {
            gsc.SetSpectatorMode();
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
        if (!canKill) return; 

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
        if (killButton != null)
        {
            killButton.interactable = false;
        }
        if (killButtonText != null)
        {
            killButtonText.text = $"{Mathf.CeilToInt(killCooldown)}초";
        }

        canKill = false; 
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
    
    public void SetMovementSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
    }

    public bool GetFlipX()
    {
        return spriteRenderer.flipX;
    }

    void OnEatButtonClicked()
    {
        GameObject corpse = FindClosestDeadBody();
        if (corpse != null)
        {
            var identifier = corpse.GetComponent<NetworkPlayerIdentifier>();
            if (identifier != null && !string.IsNullOrEmpty(identifier.playerId))
            {
                NetworkManager.Instance.socket.Emit("eatCorpse", new
                {
                    targetId = identifier.playerId
                });
            }

            Destroy(corpse);

            HungerSystem hunger = GetComponent<HungerSystem>();
            if (hunger != null)
            {
                hunger.Eat(hungerRecovery);
            }

            Debug.Log("시체를 먹었습니다!");
        }
        else
        {
            Debug.Log("근처에 먹을 수 있는 시체가 없습니다.");
        }
    }

    private GameObject FindClosestDeadBody()
    {
        float minDist = float.MaxValue;
        GameObject closest = null;

        foreach (var kvp in WaitingRoomController.otherPlayers)
        {
            string otherId = kvp.Key;
            GameObject other = kvp.Value;
            if (other == null) continue;
            if (!DeadPlayerIds.Contains(otherId)) continue;
            float dist = Vector2.Distance(transform.position, other.transform.position);
            if (dist < eatRange && dist < minDist)
            {
                minDist = dist;
                closest = other;
            }
        }

        return closest;
    }

    public bool IsDead()
    {
        return isDead;
    }

    void OnFishingButtonClicked()
    {
        if (isDead || isFishing || isFishingCooldown || !isOnFishingTile)
        {
            Debug.Log("낚시할 수 없는 상태입니다.");
            return;
        }

        isFishing = true; // 이동 금지 플래그

        GameObject FishingPanel = Instantiate(FishingPanelPrefab, Vector3.zero, Quaternion.identity);
        TMP_Text resultText = FishingPanel.transform.Find("FishingPanel/FishingText")?.GetComponent<TMP_Text>();

        if (resultText != null)
        {
            resultText.gameObject.SetActive(false); // 시작 시 숨기기
        }

        StartCoroutine(StartFishingCooldown());

        StartCoroutine(FinishFishingAfterDelay(3f, FishingPanel, resultText));

        Debug.Log(" 낚시 시작!");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == fishingLayer)
        {
            isOnFishingTile = true;
            Debug.Log("낚시 가능한 지역에 진입했습니다.");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer == fishingLayer)
        {
            isOnFishingTile = false;
            Debug.Log("낚시 가능한 지역에서 벗어났습니다.");
        }
    }

    private IEnumerator StartFishingCooldown()
    {
        isFishingCooldown = true;
        float remainingTime = fishingCooldown;

        while (remainingTime > 0f)
        {
            if (fishingCooldownText != null)
            {
                fishingCooldownText.text = $"{Mathf.CeilToInt(remainingTime)}초";
            }

            yield return new WaitForSeconds(1f);
            remainingTime -= 1f;
        }

        isFishingCooldown = false;
        if (fishingCooldownText != null)
        {
            fishingCooldownText.text = ""; // 쿨타임 종료 시 텍스트 초기화
        }
        Debug.Log("쿨타임 종료: 다시 낚시할 수 있습니다.");
    }

    private IEnumerator FinishFishingAfterDelay(float delay, GameObject panel, TMP_Text resultText)
    {
        yield return new WaitForSeconds(delay);

        
        HungerSystem hunger = GetComponent<HungerSystem>();
        hunger?.Eat(10);

        isFishing = false;

        if (resultText != null)
        {
            resultText.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(1f); // 결과 보여준 후

        if (panel != null)
        {
            Destroy(panel);
        }

        Debug.Log(" 낚시 종료: 배고픔 +10");
    }
}
