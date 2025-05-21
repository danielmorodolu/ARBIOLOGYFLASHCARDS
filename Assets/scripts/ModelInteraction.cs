using UnityEngine;

public class ModelInteraction : MonoBehaviour
{
    private float initialDistance;
    private Vector3 initialScale;
    private float initialAngle;
    private bool isInteracting;

    void Start()
    {
        // Ensure a collider exists for raycasting
        if (GetComponent<Collider>() == null)
        {
            gameObject.AddComponent<MeshCollider>();
        }
    }

    void Update()
    {
        // Tap to focus (pulse animation)
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                Ray ray = Camera.main.ScreenPointToRay(touch.position);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit) && hit.transform == transform)
                {
                    StartCoroutine(PulseAnimation(1.2f, 0.3f));
                }
            }
        }

        // Pinch-to-scale and twist-to-rotate
        if (Input.touchCount == 2)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
            {
                initialDistance = Vector2.Distance(touch0.position, touch1.position);
                initialScale = transform.localScale;
                Vector2 delta = touch1.position - touch0.position;
                initialAngle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                isInteracting = true;
            }
            else if (touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved)
            {
                if (isInteracting)
                {
                    float currentDistance = Vector2.Distance(touch0.position, touch1.position);
                    if (Mathf.Approximately(initialDistance, 0)) return;
                    float factor = currentDistance / initialDistance;
                    transform.localScale = initialScale * factor;

                    Vector2 delta = touch1.position - touch0.position;
                    float currentAngle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                    float angleDiff = currentAngle - initialAngle;
                    transform.Rotate(0, -angleDiff, 0, Space.World);
                    initialAngle = currentAngle;
                }
            }
            else if (touch0.phase == TouchPhase.Ended || touch1.phase == TouchPhase.Ended)
            {
                isInteracting = false;
            }
        }
    }

    private System.Collections.IEnumerator PulseAnimation(float targetScaleMultiplier, float duration)
    {
        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = originalScale * targetScaleMultiplier;

        float elapsed = 0f;
        while (elapsed < duration / 2)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (duration / 2);
            float easeT = t * t;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, easeT);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < duration / 2)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (duration / 2);
            float easeT = 1f - (1f - t) * (1f - t);
            transform.localScale = Vector3.Lerp(targetScale, originalScale, easeT);
            yield return null;
        }

        transform.localScale = originalScale;
    }
}