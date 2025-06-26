using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HungerSystem : MonoBehaviour
{
    public float maxHunger = 100f;
    public float hungerDecreaseRate = 1f;
    public float currentHunger;

    private bool isAlive = true;
    private bool starving = false;
    private HungerUIController uiController;

    private void Start()
    {
        currentHunger = maxHunger;
        uiController = FindObjectOfType<HungerUIController>();
        UpdateUI();
        StartCoroutine(HungerDecreaseLoop());
    }

    // 배고픔 게이지 감소
    private IEnumerator HungerDecreaseLoop()
    {
        while (isAlive)
        {
            yield return new WaitForSeconds(1f);

            currentHunger -= hungerDecreaseRate;
            currentHunger = Mathf.Clamp(currentHunger, 0, maxHunger);
            UpdateUI();

            // 배고픔이 0 이하로 떨어지면 굶주림 상태로 전환
            if (currentHunger <= 0 && !starving)
            {
                starving = true;
                uiController?.StartBlink();
                StartCoroutine(HandleStarvation());
            }
        }
    }

    private IEnumerator HandleStarvation()
    {
        // 디버프 처리 (속도 감소)
        GetComponent<CharacterMovement>()?.SetMovementSpeed(1.0f);

        yield return new WaitForSeconds(30f);

        if (isAlive)
        {
            GetComponent<CharacterMovement>()?.SetDead();
            uiController?.StopBlink();
            isAlive = false;
            NetworkManager.Instance.socket.Emit("hungerDeath", new { playerId = NetworkManager.Instance.socket.Id });
        }
    }

    // 플레이어가 음식을 먹었을 때 처리
    public void Eat(float amount)
    {
        if (!isAlive) return;

        currentHunger += amount;
        currentHunger = Mathf.Clamp(currentHunger, 0, maxHunger);
        UpdateUI();

        // 굶주림 상태에서 벗어났다면 디버프 제거
        if (starving && currentHunger > 0)
        {
            starving = false;
            uiController?.StopBlink();
            GetComponent<CharacterMovement>()?.SetMovementSpeed(3.0f);
        }
    }

    void UpdateUI()
    {
        uiController?.UpdateSlider(currentHunger / maxHunger);
    }
    
    public void Kill()
    {
        isAlive = false;

        // 슬라이더 깜빡임 멈추기
        uiController?.StopBlink();
    }
}