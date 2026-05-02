using UnityEngine;

public class SoldierMover : MonoBehaviour
{
    public float moveSpeed = 2f;     // Movement speed
    public float switchTime = 3f;    // Time before changing direction
    private bool moveRight = true;   // Initial direction
    private float timer;             // Timer to track switching

    void Start()
    {
        timer = switchTime;          // Initialize timer
    }

    void Update()
    {
        // Move the soldier along X-axis
        float direction = moveRight ? 1f : -1f;
        transform.Translate(Vector3.right * direction * moveSpeed * Time.deltaTime);

        // Countdown timer
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            moveRight = !moveRight;  // Change direction
            timer = switchTime;       // Reset timer
        }
    }
}
