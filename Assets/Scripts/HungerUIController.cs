using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HungerUIController : MonoBehaviour
{
    public Slider hungerSlider;
    public Image BackgroundImage;

    public Color normalColor = Color.white;
    public Color blinkColor = Color.red;
    public float blinkInterval = 0.5f;

    private Coroutine blinkCoroutine;

    void Start()
    {
        if (hungerSlider != null)
        {
            hungerSlider.interactable = false;
        }
    }
    public void UpdateSlider(float normalizedValue)
    {
        hungerSlider.value = normalizedValue;
    }

    public void StartBlink()
    {
        if (blinkCoroutine == null)
        {
            blinkCoroutine = StartCoroutine(BlinkFill());
        }
    }

    public void StopBlink()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        if (BackgroundImage != null)
        {
            BackgroundImage.color = normalColor;
        }
    }

    IEnumerator BlinkFill()
    {
        bool isRed = false;
        while (true)
        {
            if (BackgroundImage != null)
            {
                BackgroundImage.color = isRed ? blinkColor : normalColor;
            }
            isRed = !isRed;
            yield return new WaitForSeconds(blinkInterval);
        }
    }
    
}