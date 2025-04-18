using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;

[System.Serializable]
public class RoomInfo
{
    public int currentCnt;
    public int RoomMaxCnt;
    public string name;
}

public class LobyManager : MonoBehaviour
{
    public RoomInfo[] roomsInfo; //방정보를 배열
    
    public Transform roomParent;//생성시킬 곳
    public GameObject roomPrefab;//프리팹
    
    public List<GameObject> roomobs;

    private int _maxRoom = 4;
    
    [SerializeField] private TMP_InputField createInputField;
    public void CreateBtn()
    //방생성 버튼시 실행하는 함수
    {

        NetworkManager.Instance.socket.Emit("CreateCheck", createInputField.text, _maxRoom);
    }
    
    private void Start()
    {
    
        NetworkManager.Instance.socket.OnUnityThread("RoomReset", data =>
        //룸 리셋 ,모두가 받는 이벤트
            {
                string jsonString = data.ToString();
                
                if (data.ToString() == "[[]]")
                {
                    return;
                }
                roomsInfo = JsonConvert.DeserializeObject<RoomInfo[]>(jsonString);
                RoomReset();
            });
            
        NetworkManager.Instance.socket.OnUnityThread("RoomList", data =>
        //룸 리셋 ,개인이 받을 때 이벤트
        {
            string jsonString = data.ToString();
            
            if (data.ToString() == "[[]]")
            {
                return;
            }

            roomsInfo = JsonConvert.DeserializeObject<RoomInfo[]>(jsonString);
            //받아봅니다.
            RoomReset();
        });
    }
    
    
    public void RoomReset()
    {
        if (roomobs.Count > 0)
        {
            for (int i = 0; i < roomobs.Count; i++)
            {
                Destroy(roomobs[i]);
                //기존에 있는 방을 전부 파괴시킵니다.
                //오브젝트 풀링방법을 사용하는게 가장 베스트인데 임시로 만들었습니다.
            }
        }
        roomobs.Clear();
        
        
        
    }
}
