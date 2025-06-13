using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class WaitingRoomController : MonoBehaviour
{
    public GameObject myCharacterPrefab;
    public GameObject otherCharacterPrefab;
    public static Dictionary<string, GameObject> otherPlayers = new();
    private GameObject myCharacter;

    [Header("UI Elements")]
    public TMP_Text playerCountText;   // 방 인원 수 표시용 텍스트
    public Button startGameButton;     // 게임 시작 버튼
    public Button leaveRoomButton;     // 방 나가기 버튼

    private int currentPlayers = 0;
    private int maxPlayers = 0;

    void Start()
    {
        // 내 캐릭터 생성
        Vector3 spawnPos = new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f));
        myCharacter = Instantiate(myCharacterPrefab, spawnPos, Quaternion.identity);

        FollowCamera followCam = Camera.main.GetComponent<FollowCamera>();
        if (followCam != null)
        {
            followCam.SetTarget(myCharacter.transform);
        }

        

        // 다른 플레이어의 이동 정보 수신
        NetworkManager.Instance.socket.On("move", (data) =>
        {
            try
            {
                JArray arr = JArray.Parse(data.ToString());
                if (arr.Count == 0) return;

                JObject json = (JObject)arr[0];

                string id = json["id"]?.ToString();
                float x = json["x"]?.ToObject<float>() ?? 0;
                float y = json["y"]?.ToObject<float>() ?? 0;
                bool flipX = json["flipX"]?.ToObject<bool>() ?? false;

                string myId = NetworkManager.Instance.socket.Id;
                if (id == myId) return;

                MainThreadDispatcher.Enqueue(() =>
                {
                    if (!otherPlayers.ContainsKey(id))
                    {
                        GameObject other = Instantiate(otherCharacterPrefab, Vector3.zero, Quaternion.identity);
                        var identifier = other.GetComponent<NetworkPlayerIdentifier>();
                        if (identifier != null)
                        {
                            identifier.playerId = id;
                        }
                        otherPlayers[id] = other;
                    }

                    otherPlayers[id].transform.position = new Vector3(x, y, 0);

                    var sr = otherPlayers[id].GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.flipX = flipX;
                    }
                });
            }
            catch (System.Exception ex)
            {
                Debug.LogError("❌ move 이벤트 파싱 실패: " + ex.Message);
            }
        });

        // 내 위치 주기적 전송
        StartCoroutine(SendMoveLoop());

        // 방 인원 요청
        NetworkManager.Instance.socket.Emit("getRoomPlayerCount");
        NetworkManager.Instance.socket.On("roomPlayerCount", (data) =>
        {
            try
            {
                JArray arr = JArray.Parse(data.ToString());
                JObject json = (JObject)arr[0];
                currentPlayers = json["current"].ToObject<int>();
                maxPlayers = json["max"].ToObject<int>();

                MainThreadDispatcher.Enqueue(() =>
                {
                    UpdatePlayerCountUI();
                });
            }
            catch (System.Exception ex)
            {
                Debug.LogError("❌ roomPlayerCount 파싱 실패: " + ex.Message);
            }
        });

        // 게임 시작 버튼: 기본 비활성화
        startGameButton.gameObject.SetActive(false);

        string hostId = NetworkManager.Instance.HostId;
        bool isHost = hostId == NetworkManager.Instance.socket.Id;
        startGameButton.gameObject.SetActive(isHost);
        Debug.Log($"[WaitingRoomController] 내 ID: {NetworkManager.Instance.socket.Id}, 방장 ID: {hostId}, 방장 여부: {isHost}");
        // 게임 시작 버튼 클릭 이벤트
        startGameButton.onClick.AddListener(OnStartGameClicked);

        // 방 나가기 버튼
        leaveRoomButton.onClick.AddListener(OnLeaveRoomClicked);

        // 게임 시작 이벤트 수신
        NetworkManager.Instance.socket.On("gameStarted", (_) =>
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                Debug.Log("게임 시작됨!");
                SceneManager.LoadScene("Game");
            });
        });

        // 방 참가 시 방장 정보 수신 → 버튼 제어
        NetworkManager.Instance.socket.On("joinedRoom", (data) =>
        {
            JObject json = JObject.Parse(data.ToString());
            string hostId = json["hostId"]?.ToString();
            NetworkManager.Instance.SetHostId(hostId);

            MainThreadDispatcher.Enqueue(() =>
            {
                bool isHost = (hostId == NetworkManager.Instance.socket.Id);
                startGameButton.gameObject.SetActive(isHost);
            });
        });

        // 방장 변경 시 버튼 갱신
        NetworkManager.Instance.socket.On("hostChanged", (data) =>
        {
            JObject json = JObject.Parse(data.ToString());
            string newHostId = json["hostId"]?.ToString();
            NetworkManager.Instance.SetHostId(newHostId);

            MainThreadDispatcher.Enqueue(() =>
            {
                bool isHost = (newHostId == NetworkManager.Instance.socket.Id);
                startGameButton.gameObject.SetActive(isHost);
            });
        });
    }

    private void UpdatePlayerCountUI()
    {
        playerCountText.text = $"방 인원: {currentPlayers}/{maxPlayers}";
    }

    private void OnStartGameClicked()
    {
        NetworkManager.Instance.socket.Emit("startGame");
    }

    public void OnLeaveRoomClicked()
    {
        Debug.Log("방 나가기 요청");

        NetworkManager.Instance.socket.Emit("leaveRoom", new
        {
            roomId = NetworkManager.Instance.socket.Id
        });

        SceneManager.LoadScene("Main");
    }

    IEnumerator SendMoveLoop()
    {
        while (true)
        {
            var pos = myCharacter.transform.position;
            bool flipX = myCharacter.GetComponent<CharacterMovement>().GetFlipX();

            NetworkManager.Instance.socket.Emit("move", new
            {
                x = pos.x,
                y = pos.y,
                flipX = flipX
            });

            yield return new WaitForSeconds(0.1f); // 10fps
        }
    }

    [System.Serializable]
    public class MoveData
    {
        public string id;
        public float x;
        public float y;
    }
}