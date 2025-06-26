using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.GraphicsBuffer;

public class GameSceneController : MonoBehaviour
{
    [SerializeField] private TMP_Text spectatorHintText;
    [SerializeField] private GameObject gameResultPanelPrefab;
    private GameObject currentResultPanel;

    public GameObject myCharacterPrefab;
    public GameObject otherCharacterPrefab;
    private GameObject myCharacter;

    private List<string> aliveSpectateIds = new List<string>();
    private int currentSpectateIndex = 0;
    private bool isSpectating = false;

    void Start()
    {
        if (spectatorHintText != null)
            spectatorHintText.gameObject.SetActive(false);

        Vector3 spawnPos = new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f));

        myCharacter = Instantiate(myCharacterPrefab, spawnPos, Quaternion.identity);

        FollowCamera followCam = Camera.main.GetComponent<FollowCamera>();
        if (followCam != null)
        {
            followCam.SetTarget(myCharacter.transform);
        }

        //캐릭터 움직임 처리
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
                            return;
                        }

                        GameObject other = Instantiate(otherCharacterPrefab, Vector3.zero, Quaternion.identity);
                        var identifier = other.GetComponent<NetworkPlayerIdentifier>();
                        if (identifier != null)
                        {
                            identifier.playerId = id;
                        }

                        WaitingRoomController.otherPlayers[id] = other;
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
                Debug.LogError(" move 이벤트 파싱 실패: " + ex.Message);
            }
        });

        //게임 종료 처리
        NetworkManager.Instance.socket.On("gameEnded", (data) =>
        {
            JArray arr = JArray.Parse(data.ToString());
            if (arr.Count == 0) return;

            JObject json = (JObject)arr[0];
            string winner = json["winner"]?.ToString();

            MainThreadDispatcher.Enqueue(() =>
            {
                ShowResultPanel(winner);
                StartCoroutine(ReturnToWaitingRoomAfterDelay(5f));
            });
        });

        StartCoroutine(SendMoveLoop());
    }
    
    IEnumerator SendMoveLoop()
    {
        while (true)
        {
            if (myCharacter == null) yield break;
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

    void Update()
    {
        if (!isSpectating) return;

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            CycleSpectate(1);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            CycleSpectate(-1);
        }
    }

    public void SetSpectatorMode()
    {
        aliveSpectateIds.Clear();
        foreach (var kvp in WaitingRoomController.otherPlayers)
        {
            if (!CharacterMovement.DeadPlayerIds.Contains(kvp.Key))
                aliveSpectateIds.Add(kvp.Key);
        }

        if (aliveSpectateIds.Count == 0) return;

        currentSpectateIndex = 0;
        isSpectating = true;
        SetSpectateTarget(aliveSpectateIds[currentSpectateIndex]);

        if (spectatorHintText != null)
            spectatorHintText.gameObject.SetActive(true);
    }

    private void CycleSpectate(int delta)
    {
        if (aliveSpectateIds.Count == 0) return;

        currentSpectateIndex = (currentSpectateIndex + delta + aliveSpectateIds.Count) % aliveSpectateIds.Count;
        SetSpectateTarget(aliveSpectateIds[currentSpectateIndex]);
    }

    private void SetSpectateTarget(string id)
    {
        if (WaitingRoomController.otherPlayers.TryGetValue(id, out GameObject target))
        {
            var cam = Camera.main.GetComponent<FollowCamera>();
            cam?.SetTarget(target.transform);
        }
    }

    private void ShowResultPanel(string winner)
    {
        if (gameResultPanelPrefab == null)
        {
            Debug.LogError("결과 패널 프리팹이 설정되지 않았습니다.");
            return;
        }

        currentResultPanel = Instantiate(gameResultPanelPrefab, Vector3.zero, Quaternion.identity);
        TMP_Text resultText = currentResultPanel.transform.Find("GameResultPanel/ResultText")?.GetComponent<TMP_Text>();

        if (resultText != null)
        {
            if (winner == "Imposter")
                resultText.text = "임포스터 승리!";
            else if (winner == "Crew")
                resultText.text = "일반인 승리!";
            else
                resultText.text = "게임 종료";
        }
    }

    private IEnumerator ReturnToWaitingRoomAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        SceneManager.LoadScene("Waiting Room");
    }
}

