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

    private IEnumerator HungerDecreaseLoop()
    {
        while (isAlive)
        {
            yield return new WaitForSeconds(1f);

            currentHunger -= hungerDecreaseRate;
            currentHunger = Mathf.Clamp(currentHunger, 0, maxHunger);
            UpdateUI();

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
        // 디버프 처리 (속도 감소 등)
        GetComponent<CharacterMovement>()?.SetMovementSpeed(1.0f);

        yield return new WaitForSeconds(30f);

        if (isAlive)
        {
            GetComponent<CharacterMovement>()?.SetDead();
            uiController?.StopBlink();
            isAlive = false;
        }
    }

    public void Eat(float amount)
    {
        if (!isAlive) return;

        currentHunger += amount;
        currentHunger = Mathf.Clamp(currentHunger, 0, maxHunger);
        UpdateUI();

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