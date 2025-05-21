using UnityEngine;

public class HeartbeatAnimation : MonoBehaviour
{
    public float pulseSpeed = 1f; // Speed of the heartbeat (pulses per second)
    public float pulseMagnitude = 0.03f; // How much the model scales up/down
    private Vector3 baseScale; // The model's original scale
    private float timeOffset; // Random offset to desync multiple models

    private void Start()
    {
        // Store the initial scale of the model
        baseScale = transform.localScale;
        // Randomize the starting point of the animation to avoid syncing
        timeOffset = Random.Range(0f, 2f * Mathf.PI);
    }

    private void Update()
    {
        // Use a sine wave to create a smooth pulsing effect
        float pulse = Mathf.Sin((Time.time + timeOffset) * pulseSpeed * 2f * Mathf.PI) * pulseMagnitude;
        // Apply the pulse to the scale
        transform.localScale = baseScale + new Vector3(pulse, pulse, pulse);
    }
}