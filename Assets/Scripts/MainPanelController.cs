using System.Collections;
using System.Collections.Generic;
using SocketIOClient;
using UnityEngine;

public class MainPanelController : MonoBehaviour
{
    private string _roomId;
    
    public void OnClickCreateRoomButton()
    {
        Debug.Log("OnClickCreateRoomButton");
        
        NetworkManager.Instance.socket.Emit("createRoom");
        
        NetworkManager.Instance.socket.On("createdRoom", (e) =>
        {
            Debug.Log("새로운 방 생성됨 : " + e);
            _roomId = e.ToString();
            Debug.Log(_roomId);
        });
        
        NetworkManager.Instance.socket.On("errorJoin", (e) => {
            Debug.LogError("방 접속 실패: " + e);
        });
    }

    public void OnClickJoinRoomButton()
    {
        Debug.Log("OnClickJoinRoomButton");
        Debug.Log(_roomId);
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
    
    // public void OnClickGetRoomsButton()
    // {
    //     Debug.Log("OnClickGetRoomsButton");
    //
    //     // 방 목록을 요청
    //     NetworkManager.Instance.socket.Emit("getRooms");
    //
    //     NetworkManager.Instance.socket.On("roomList", (e) =>
    //     {
    //         List<string> rooms = new List<string>();
    //         foreach (var room in e.Json["rooms"])
    //         {
    //             rooms.Add(room.ToString());
    //         }
    //
    //         Debug.Log("현재 방 목록:");
    //         foreach (var room in rooms)
    //         {
    //             Debug.Log(room);  // 방 목록 출력
    //         }
    //     });
    // }
}
