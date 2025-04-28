using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SocketIOClient;
using UnityEngine;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
    public string roomName;
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
    private string _roomName;
    
    [SerializeField] private Transform roomListParent;         // 생성 위치
    [SerializeField] private GameObject roomPanelPrefab;
    [SerializeField] private GameObject roomEntryPrefab;
    [SerializeField] private GameObject createRoomPanelPrefab;
    [SerializeField] private Transform createRoomPanelParent;  
    
    public void OnClickCreateRoomButton()
    {
        Instantiate(createRoomPanelPrefab, createRoomPanelParent);
    }
    
    public void OnClickGetRoomListButton()
    {
        NetworkManager.Instance.socket.Emit("getRoomList");

        NetworkManager.Instance.socket.On("roomList", (data) =>
        {
            try
            {
                string json = data.ToString();
                List<RoomInfo> parsedRooms = new();

                if (json.TrimStart().StartsWith("["))
                {
                    JArray rawArray = JArray.Parse(json);       // 전체가 이중 배열
                    JArray roomArray = (JArray)rawArray[0];     // 내부 진짜 room 배열

                    foreach (var token in roomArray)
                    {
                        if (token.Type == JTokenType.Object)
                        {
                            JObject roomObj = (JObject)token;

                            RoomInfo room = new RoomInfo
                            {
                                roomName = roomObj["roomName"]?.ToString(),
                                roomId = roomObj["roomId"]?.ToString(),
                                current = roomObj["current"]?.ToObject<int>() ?? 0,
                                max = roomObj["max"]?.ToObject<int>() ?? 0
                            };

                            parsedRooms.Add(room);
                        }
                    }
                }
                else
                {
                    
                    JObject root = JObject.Parse(json);
                    JArray roomArray = (JArray)root["rooms"];

                    foreach (JToken token in roomArray)
                    {
                        if (token is JObject roomToken)
                        {
                            RoomInfo room = new RoomInfo
                            {
                                roomName = roomToken["roomName"]?.ToString(),
                                roomId = roomToken["roomId"]?.ToString(),
                                current = roomToken["current"]?.ToObject<int>() ?? 0,
                                max = roomToken["max"]?.ToObject<int>() ?? 0
                            };

                            parsedRooms.Add(room);
                        }
                    }
                }
                
                MainThreadDispatcher.Enqueue(() =>
                {
                    // RoomPanelPrefab 하나만 생성
                    GameObject roomPanel = Instantiate(roomPanelPrefab, roomListParent);

                    // ✅ 닫기 버튼 핸들링
                    Button closeButton = roomPanel.transform.Find("CloseButton")?.GetComponent<Button>();
                    if (closeButton != null)
                    {
                        closeButton.onClick.AddListener(() =>
                        {
                            Destroy(roomPanel);  // 방 리스트 UI 제거
                        });
                    }

                    Transform listContainer = roomPanel.transform.Find("RoomListContainer");
                    if (listContainer == null)
                    {
                        Debug.LogError("RoomListContainer를 찾을 수 없습니다.");
                        return;
                    }

                    foreach (RoomInfo room in parsedRooms)
                    {
                        GameObject entry = Instantiate(roomEntryPrefab, listContainer);

                        TMP_Text text = entry.transform.Find("RoomText").GetComponent<TMP_Text>();
                        Button joinButton = entry.transform.Find("JoinButton").GetComponent<Button>();

                        text.text = $"{room.roomName} ({room.current} / {room.max})";
                        string selectedRoomId = room.roomId;
                        joinButton.onClick.AddListener(() =>
                        {
                            NetworkManager.Instance.socket.Emit("joinRoom", new { roomId = selectedRoomId });
                            
                            NetworkManager.Instance.socket.On("joinedRoom", (e) => { });

                            SceneManager.LoadScene("Waiting Room");
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.LogError("방 목록 파싱 오류: " + ex.Message);
            }
        });
    }
    
}
