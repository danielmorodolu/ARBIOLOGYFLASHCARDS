using UnityEngine;
using UnityEngine.UI;

public class ButtonAnimation : MonoBehaviour
{
    private Button button;
    private Vector3 originalScale;

    private void Awake()
    {
        button = GetComponent<Button>();
        originalScale = transform.localScale;

        if (button != null)
        {
            button.onClick.AddListener(AnimateButton);
        }
    }

    private void AnimateButton()
    {
        // Reset scale to original
        transform.localScale = originalScale;

        // Animate scale: grow to 1.2x then back to original
        LeanTween.scale(gameObject, originalScale * 1.2f, 0.2f)
            .setEase(LeanTweenType.easeOutQuad)
            .setOnComplete(() =>
            {
                LeanTween.scale(gameObject, originalScale, 0.2f)
                    .setEase(LeanTweenType.easeInQuad);
            });
    }
}