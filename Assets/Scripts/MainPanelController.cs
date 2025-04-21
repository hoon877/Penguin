using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SocketIOClient;
using UnityEngine;
using Newtonsoft.Json.Linq;
using TMPro;

[System.Serializable]
public class RoomIdData
{
    public string roomId;
}

[System.Serializable]
public class RoomIdDataArray
{
    public RoomIdData[] items;
}

[System.Serializable]
public class RoomInfo
{
    public string roomId;
    public int current;
    public int max;
}

[System.Serializable]
public class RoomListResponse
{
    public List<RoomInfo> rooms;
}

public class MainPanelController : MonoBehaviour
{
    private string _roomId;
    
    [SerializeField] private Transform roomListParent;         // 생성 위치
    [SerializeField] private GameObject roomPanelPrefab;
    
    public void OnClickCreateRoomButton()
    {
        Debug.Log("OnClickCreateRoomButton");
        
        NetworkManager.Instance.socket.Emit("createRoom");

        NetworkManager.Instance.socket.On("createdRoom", (data) =>
        {
            string jsonString = data.ToString();

            // JSON 배열 형태로 처리하기 위해 직접 배열 형태로 감싸줌
            jsonString = "{\"items\":" + jsonString + "}";

            RoomIdDataArray roomDataArray = JsonUtility.FromJson<RoomIdDataArray>(jsonString);

            if (roomDataArray.items.Length > 0)
            {
                _roomId = roomDataArray.items[0].roomId;
                Debug.Log("새로운 방 생성됨: " + _roomId);
            }
            else
            {
                Debug.LogError("방 ID 데이터가 비어있습니다.");
            }
        });

        NetworkManager.Instance.socket.On("errorJoin", (e) => {
            Debug.LogError("방 접속 실패: " + e);
        });
    }

    public void OnClickJoinRoomButton()
    {
        Debug.Log("OnClickJoinRoomButton");
        if (!string.IsNullOrEmpty(_roomId))
        {
            // 방 ID가 있을 때만 joinRoom 이벤트를 보냄
            NetworkManager.Instance.socket.Emit("joinRoom", new { roomId = _roomId });

            NetworkManager.Instance.socket.On("joinedRoom", (e) =>
            {
                Debug.Log("joinedRoom");
            });
        }
        else
        {
            Debug.LogError("방 ID가 없습니다. 먼저 방을 생성해주세요.");
        }
    }
    
    public void OnClickGetRoomListButton()
    {
        Debug.Log("방 목록 요청");

        NetworkManager.Instance.socket.Emit("getRoomList");

        NetworkManager.Instance.socket.On("roomList", (data) =>
        {
            string json = data.ToString();
            
            Debug.Log("받은 JSON: " + json);
            json = "{\"rooms\":" + json + "}";
            Debug.Log("JSon :" +  json);
            
            RoomListResponse result = JsonUtility.FromJson<RoomListResponse>(json);
            
            if (result == null || result.rooms == null || result.rooms.Count == 0)
            {
                Debug.Log("⚠ 방이 없습니다.");
                return;
            }
            
            // Debug.Log(result.rooms[0].roomId);
            
            // 기존 UI 제거
            // foreach (Transform child in roomListParent)
            // {
            //     Debug.Log(2);
            //     Destroy(child.gameObject);
            // }
            Debug.Log(result.rooms.Count);
            Debug.Log(3);
            
            foreach (RoomInfo room in result.rooms)
            {
                Debug.Log($"[room] roomId: {room.roomId}, current: {room.current}, max: {room.max}");
                if (roomPanelPrefab == null)
                {
                    Debug.LogError("roomPanelPrefab이 null입니다!");
                    continue;
                }

                if (roomListParent == null)
                {
                    Debug.LogError("roomListParent가 null입니다!");
                    continue;
                }
                
                GameObject roomPanel = Instantiate(roomPanelPrefab, roomListParent);
                Debug.Log(4);
                
                // // 자식 요소에서 텍스트/버튼 찾기
                // TMP_Text roomIdText = roomPanel.transform.Find("RoomIdText").GetComponent<TMP_Text>();
                // TMP_Text playerCountText = roomPanel.transform.Find("PlayerCountText").GetComponent<TMP_Text>();
                // UnityEngine.UI.Button joinButton = roomPanel.transform.Find("JoinButton").GetComponent<UnityEngine.UI.Button>();
                //
                // roomIdText.text = room.roomId;
                // playerCountText.text = $"{room.current} / {room.max}";
                //
                // joinButton.onClick.AddListener(() =>
                // {
                //     NetworkManager.Instance.socket.Emit("joinRoom", new { roomId = room.roomId });
                // });
            }
        });
    }
    
}
