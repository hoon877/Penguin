using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public class WaitingRoomController : MonoBehaviour
{
    public GameObject myCharacterPrefab;
    public GameObject otherCharacterPrefab;
    public static Dictionary<string, GameObject> otherPlayers = new();
    private GameObject myCharacter;

    [Header("UI Elements")]
    public TMP_Text playerCountText;   // 방 인원 수 표시용 텍스트
    public Button startGameButton;     // 게임 시작 버튼
    public Button leaveRoomButton; //방 나가기 버튼

    private int currentPlayers = 0;
    private int maxPlayers = 0;
    
    void Start()
    {
        // 1. 내 캐릭터 생성
        Vector3 spawnPos = new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f));
        myCharacter = Instantiate(myCharacterPrefab, spawnPos, Quaternion.identity);

        
        
        //2.서버에 "move" 이벤트 등록
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
                             Debug.Log("id : " + id);
                             Debug.Log("indentifier id : " + identifier.playerId);
                         }
                         otherPlayers[id] = other;
                         Debug.Log($"🟢 상대 캐릭터 생성: {id}");
                     }
                     
                     otherPlayers[id].transform.position = new Vector3(x, y, 0);
                 });
             }
             catch (System.Exception ex)
             {
                 Debug.LogError("❌ move 이벤트 파싱 실패: " + ex.Message);
             }
         });

        // 3. 이동 시작 (예: 키 입력으로 move 이벤트 emit)
        StartCoroutine(SendMoveLoop());


        // 현재 방 인원 요청 및 UI 업데이트
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
                Debug.LogError("받은 원본 데이터: " + data);
            }
        });

        // 게임 시작 버튼 이벤트 등록
        startGameButton.onClick.AddListener(OnStartGameClicked);

        // 게임 시작 이벤트 리스너
        NetworkManager.Instance.socket.On("gameStarted", (_) =>
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                Debug.Log("게임 시작됨!");
                SceneManager.LoadScene("Game"); // 게임 씬으로 이동
            });
        });

        // 방 나가기 버튼 이벤트 등록
        leaveRoomButton.onClick.AddListener(OnLeaveRoomClicked);
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
            roomId = NetworkManager.Instance.socket.Id // 또는 저장해 둔 roomId
        });

        // 클라이언트가 먼저 씬을 나가도 괜찮음 (서버는 나중에 처리됨)
        SceneManager.LoadScene("Main"); // 또는 로비 씬 이름
    }

    IEnumerator SendMoveLoop()
    {
        while (true)
        {
            var pos = myCharacter.transform.position;

            NetworkManager.Instance.socket.Emit("move", new
            {
                x = pos.x,
                y = pos.y
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
