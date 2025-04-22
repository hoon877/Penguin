using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaitingRoomController : MonoBehaviour
{
    public GameObject myCharacterPrefab;
    public GameObject otherCharacterPrefab;

    private Dictionary<string, GameObject> otherPlayers = new();

    void Start()
    {
        // 1. 내 캐릭터 생성
        Vector3 spawnPos = new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f));
        Instantiate(myCharacterPrefab, spawnPos, Quaternion.identity);

        // 2. 서버에 "move" 이벤트 등록
        // NetworkManager.Instance.socket.On("move", (data) =>
        // {
        //     var json = data.ToString();
        //     var moveData = JsonUtility.FromJson<MoveData>(json);
        //
        //     if (!otherPlayers.ContainsKey(moveData.id))
        //     {
        //         // 새로운 상대 생성
        //         GameObject other = Instantiate(otherCharacterPrefab, Vector3.zero, Quaternion.identity);
        //         otherPlayers[moveData.id] = other;
        //     }
        //
        //     // 상대 위치 갱신
        //     otherPlayers[moveData.id].transform.position = new Vector3(moveData.x, 0, moveData.y);
        // });

        // // 3. 이동 시작 (예: 키 입력으로 move 이벤트 emit)
        // StartCoroutine(SendMoveLoop());
    }

    // IEnumerator SendMoveLoop()
    // {
    //     while (true)
    //     {
    //         var myX = transform.position.x;
    //         var myY = transform.position.z;
    //
    //         NetworkManager.Instance.socket.Emit("move", new {
    //             x = myX,
    //             y = myY
    //         });
    //
    //         yield return new WaitForSeconds(0.1f); // 10fps
    //     }
    // }
    //
    // [System.Serializable]
    // public class MoveData
    // {
    //     public string id;
    //     public float x;
    //     public float y;
    // }
}
