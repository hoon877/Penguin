using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CharacterMovement : MonoBehaviour
{
    [SerializeField] GameObject DeadPanelPrefab;
    [SerializeField] Transform target; 
    [SerializeField] float moveSpeed = 3.0f;
    [SerializeField] float killRange = 10.0f;
    
    private List<string> otherPlayerIds = new List<string>();
    private string myId;
    
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
    
    void Start()
    {
        // 내 플레이어 ID
        myId = NetworkManager.Instance.socket.Id;
        // 서버에 요청
        NetworkManager.Instance.socket.Emit("getPlayers");

        // 응답 수신
        NetworkManager.Instance.socket.On("playersInRoom", (data) =>
        {
            JArray arr = JArray.Parse(data.ToString());
            otherPlayerIds.Clear();

            foreach (var idToken in arr)
            {
                string id = idToken.ToString();
                if (id != myId)
                {
                    otherPlayerIds.Add(id);
                }
            }

            Debug.Log("👥 상대 ID 목록 받아옴: " + string.Join(", ", otherPlayerIds));
        });
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
            animator.SetBool("Walk", false); 
        }

        Kill();

    }

    private void Kill()
    {
        if (Input.GetMouseButtonDown(0) && !isDead)
        {
            GameObject closest = FindClosestKillableTarget();
            Debug.Log("🔍 otherPlayers count: " + WaitingRoomController.otherPlayers.Count);
            Debug.Log(closest);
            if (closest == null) return;
            var identifier = closest.GetComponent<NetworkPlayerIdentifier>();
            if (identifier == null || string.IsNullOrEmpty(identifier.playerId)) return;
            string targetId = identifier.playerId;
            Debug.Log("🗡 Target ID: " + targetId);

            NetworkManager.Instance.socket.Emit("kill", new { targetId });
            NetworkManager.Instance.socket.On("killed", (data) =>
            {
                JObject json = JObject.Parse(data.ToString());
                string victimId = json["victimId"]?.ToString();
                string killerId = json["killerId"]?.ToString();
                myId = NetworkManager.Instance.socket.Id;
                MainThreadDispatcher.Enqueue(() => { Debug.Log(3); });
                MainThreadDispatcher.Enqueue(() =>
                {
                    if (victimId == myId)
                    {
                        Debug.Log(2);
                        isDead = true;
                        GameObject panelInstance = Instantiate(DeadPanelPrefab, Vector3.zero, Quaternion.identity);
                        Destroy(panelInstance, seconds);
                        StartCoroutine(DeadAfterDelay());
                    }
                    else
                    {
                        if (WaitingRoomController.otherPlayers.TryGetValue(victimId, out GameObject victimGo))
                        {
                            CharacterMovement victimMovement = victimGo.GetComponent<CharacterMovement>();
                            if (victimMovement != null)
                            {
                                victimMovement.SetDead();
                            }

                            Debug.Log(1);
                            GameObject panelInstance = Instantiate(DeadPanelPrefab, victimGo.transform.position, Quaternion.identity);
                            Destroy(panelInstance, seconds);
                        }
                    }
                });
                Debug.Log(4);
            });
        }
    }

    private bool IsInKillRange()
    {
        float distance = Vector2.Distance(transform.position, target.position);
        return distance <= killRange;
    }

    IEnumerator DeadAfterDelay()
    {
        yield return new WaitForSeconds(seconds);
        isDead = false;
    }
    
    public void SetDead()
    {
        isDead = true;
        animator.SetBool("Walk", false);
    }
    
    private GameObject FindClosestKillableTarget()
    {
        float minDist = float.MaxValue;
        GameObject closest = null;

        foreach (var kvp in WaitingRoomController.otherPlayers)
        {
            GameObject other = kvp.Value;
            if (other == null)
            {
                Debug.Log($"⚠️ otherPlayers[{kvp.Key}] is null");
                continue;
            }

            float dist = Vector2.Distance((Vector2)transform.position, (Vector2)other.transform.position);
            Debug.Log($"📏 검사 대상: {kvp.Key}, 거리: {dist}, killRange: {killRange}");
            if (dist < killRange && dist < minDist)
            {
                minDist = dist;
                closest = other;
            }
        }

        if (closest == null)
            Debug.Log("❌ 킬 대상 없음 (모든 대상이 killRange 밖)");

        return closest;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, killRange);
    }
}
