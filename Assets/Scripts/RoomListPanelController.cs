using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;



public class RoomListPanelController : MonoBehaviour
{
    // [SerializeField] private Transform roomListParent;         // 생성 위치
    // [SerializeField] private GameObject roomPanelPrefab; 
    //
    // public void OnClickGetRoomListButton()
    // {
    //     Debug.Log("방 목록 요청");
    //
    //     NetworkManager.Instance.socket.Emit("getRoomList");
    //
    //     NetworkManager.Instance.socket.On("roomList", (data) =>
    //     {
    //         string json = data.ToString();
    //         
    //         Debug.Log("받은 JSON: " + json);
    //         json = "{\"rooms\":" + json + "}";
    //         Debug.Log("JSon :" +  json);
    //         
    //         RoomListResponse result = JsonUtility.FromJson<RoomListResponse>(json);
    //         
    //         if (result == null || result.rooms == null || result.rooms.Count == 0)
    //         {
    //             Debug.Log("⚠ 방이 없습니다.");
    //             return;
    //         }
    //         
    //         Debug.Log(result.rooms[0].roomId);
    //         
    //         // 기존 UI 제거
    //         // foreach (Transform child in roomListParent)
    //         // {
    //         //     Debug.Log(2);
    //         //     Destroy(child.gameObject);
    //         // }
    //         
    //         Debug.Log(3);
    //         
    //         foreach (RoomInfo room in result.rooms)
    //         {
    //             GameObject roomPanel = Instantiate(roomPanelPrefab, roomListParent);
    //             Debug.Log(4);
    //
    //             // 자식 요소에서 텍스트/버튼 찾기
    //             TMP_Text roomIdText = roomPanel.transform.Find("RoomIdText").GetComponent<TMP_Text>();
    //             TMP_Text playerCountText = roomPanel.transform.Find("PlayerCountText").GetComponent<TMP_Text>();
    //             UnityEngine.UI.Button joinButton = roomPanel.transform.Find("JoinButton").GetComponent<UnityEngine.UI.Button>();
    //
    //             roomIdText.text = room.roomId;
    //             playerCountText.text = $"{room.current} / {room.max}";
    //
    //             joinButton.onClick.AddListener(() =>
    //             {
    //                 NetworkManager.Instance.socket.Emit("joinRoom", new { roomId = room.roomId });
    //             });
    //         }
    //     });
    // }
}
