using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CreateRoomPanelController : MonoBehaviour
{
    private string _roomId;
    private string _roomName;
    private int _maxPlayers;
    public TMP_InputField roomNameInput;
    public TMP_Dropdown maxPlayerDropdown;
    [SerializeField] private GameObject targetPanelToDestroy;
    
    public void OnClickCreateRoomButton()
    {
        _roomName = roomNameInput.text;
        _maxPlayers = maxPlayerDropdown.value;
        
        NetworkManager.Instance.socket.Emit("createRoom", new {
            roomName = roomNameInput.text,
            maxPlayers = int.Parse(maxPlayerDropdown.options[maxPlayerDropdown.value].text)
        });

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
        
        SceneManager.LoadScene("Waiting Room");
    }
    
}
