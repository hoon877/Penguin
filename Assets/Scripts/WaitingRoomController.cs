using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class WaitingRoomController : MonoBehaviour
{
    public GameObject myCharacterPrefab;
    public GameObject otherCharacterPrefab;

    private Dictionary<string, GameObject> otherPlayers = new();
    private GameObject myCharacter;

    void Start()
    {
        // 1. 내 캐릭터 생성
        Vector3 spawnPos = new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f));
        myCharacter = Instantiate(myCharacterPrefab, spawnPos, Quaternion.identity);

        string myId = NetworkManager.Instance.socket.Id;

        //2.서버에 "move" 이벤트 등록
        NetworkManager.Instance.socket.On("move", (data) =>
         {
             Debug.Log("move event received: " + data.ToString());
             Debug.Log(6);
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
                         otherPlayers[id] = other;
                         Debug.Log($"🟢 상대 캐릭터 생성: {id}");
                     }
                     
                     otherPlayers[id].transform.position = new Vector3(x, 0, y);
                 });
             }
             catch (System.Exception ex)
             {
                 Debug.LogError("❌ move 이벤트 파싱 실패: " + ex.Message);
             }
         });

        // 3. 이동 시작 (예: 키 입력으로 move 이벤트 emit)
        StartCoroutine(SendMoveLoop());
    }

    IEnumerator SendMoveLoop()
    {
        while (true)
        {
            var pos = myCharacter.transform.position;

            NetworkManager.Instance.socket.Emit("move", new
            {
                x = pos.x,
                y = pos.z
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
