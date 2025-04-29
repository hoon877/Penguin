using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HungerSystem : MonoBehaviour
{
    [Header("Hunger Config")]
    public float maxHunger = 100f;
    public float hungerDecreaseRate = 1f;
    public float currentHunger;

    [Header("UI")]
    private Slider hungerSlider; // -> 이건 Game 씬 Canvas 안에 있는 슬라이더를 연결해야 함

    private bool isAlive = true;

    private void Start()
    {
        currentHunger = maxHunger;

        if (hungerSlider == null)
        {
            // GameScene 안에 있는 "HungerSlider" 오브젝트를 찾아 자동으로 연결
            GameObject sliderObj = GameObject.Find("HungerSlider");
            if (sliderObj != null)
            {
                hungerSlider = sliderObj.GetComponent<Slider>();
            }
            else
            {
                Debug.LogWarning("❗ HungerSlider를 찾지 못했습니다. 이름 확인 필요.");
            }
        }

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

            if (currentHunger <= 0)
            {
                Debug.Log($"{gameObject.name}: 배고픔이 0! 추가 효과 처리 가능.");
                // 예: 체력 감소 시작
            }
        }
    }

    private void UpdateUI()
    {
        if (hungerSlider != null)
        {
            hungerSlider.value = currentHunger / maxHunger;
        }
    }

    public void Eat(float amount)
    {
        currentHunger += amount;
        currentHunger = Mathf.Clamp(currentHunger, 0, maxHunger);
        UpdateUI();
    }

    public void Kill()
    {
        isAlive = false;
    }
}