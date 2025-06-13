using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class GameSceneController : MonoBehaviour
{
    public GameObject myCharacterPrefab;
    public GameObject otherCharacterPrefab;
    private GameObject myCharacter;

    void Start()
    {
        // 1. 내 캐릭터 생성
        Vector3 spawnPos = new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f));
        myCharacter = Instantiate(myCharacterPrefab, spawnPos, Quaternion.identity);

        // ✅ 생성 직후 카메라에 타겟 설정
        FollowCamera followCam = Camera.main.GetComponent<FollowCamera>();
        if (followCam != null)
        {
            followCam.SetTarget(myCharacter.transform);
        }

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
                bool flipX = json["flipX"]?.ToObject<bool>() ?? false;

                string myId = NetworkManager.Instance.socket.Id;
                if (id == myId) return;

                MainThreadDispatcher.Enqueue(() =>
                {
                    if (!WaitingRoomController.otherPlayers.ContainsKey(id))
                    {
                        if (CharacterMovement.DeadPlayerIds.Contains(id))
                        {
                            Debug.LogWarning($"[무시됨] {id}는 이미 먹힌 시체입니다");
                            return;
                        }

                        GameObject other = Instantiate(otherCharacterPrefab, Vector3.zero, Quaternion.identity);
                        var identifier = other.GetComponent<NetworkPlayerIdentifier>();
                        if (identifier != null)
                        {
                            identifier.playerId = id;
                            Debug.Log("id : " + id);
                            Debug.Log("indentifier id : " + identifier.playerId);
                        }

                        WaitingRoomController.otherPlayers[id] = other;
                        Debug.Log($"🟢 상대 캐릭터 생성: {id}");
                    }

                    WaitingRoomController.otherPlayers[id].transform.position = new Vector3(x, y, 0);

                    var sr = WaitingRoomController.otherPlayers[id].GetComponent<SpriteRenderer>();
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

        NetworkManager.Instance.socket.On("corpseEaten", (data) =>
        {
            try
            {
                JArray arr = JArray.Parse(data.ToString());
                if (arr.Count == 0) return;

                JObject json = (JObject)arr[0];
                string targetId = json["targetId"]?.ToString();

                if (string.IsNullOrEmpty(targetId)) return;

                MainThreadDispatcher.Enqueue(() =>
                {
                    if (WaitingRoomController.otherPlayers.TryGetValue(targetId, out GameObject corpse))
                    {
                        Destroy(corpse);
                        WaitingRoomController.otherPlayers.Remove(targetId);
                        CharacterMovement.DeadPlayerIds.Remove(targetId);

                        Debug.Log($"[동기화] 시체 제거됨: {targetId}");
                    }
                });
            }
            catch (System.Exception ex)
            {
                Debug.LogError("corpseEaten 이벤트 파싱 실패: " + ex.Message);
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
}
