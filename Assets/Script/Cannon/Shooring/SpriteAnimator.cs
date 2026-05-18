using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SpriteAnimator : MonoBehaviour
{
    [Header("Frames")]
    public Sprite[] frames;
    public float fps = 12f;
    public bool loop = false;

    [Header("Spawn Frame")]
    [Tooltip("Projectile spawns on this frame index (0 = first frame)")]
    public int spawnOnFrame = 3;

    private Image image;
    private Coroutine playRoutine;

    public Action onSpawnFrame;
    public Action onComplete;

    void Awake()
    {
        image = GetComponent<Image>();

        if (frames != null && frames.Length > 0)
            image.sprite = frames[0];
    }

    public void Play()
    {
        if (playRoutine != null)
            StopCoroutine(playRoutine);

        playRoutine = StartCoroutine(Animate());
    }

    public void Stop()
    {
        if (playRoutine != null)
            StopCoroutine(playRoutine);

        if (frames != null && frames.Length > 0)
            image.sprite = frames[0];
    }

    public float GetDuration()
    {
        if (frames == null || frames.Length == 0 || fps <= 0) return 0f;
        return frames.Length / fps;
    }

    IEnumerator Animate()
    {
        float delay = 1f / fps;

        for (int i = 0; i < frames.Length; i++)
        {
            image.sprite = frames[i];

            if (i == spawnOnFrame)
                onSpawnFrame?.Invoke();

            yield return new WaitForSeconds(delay);
        }

        onComplete?.Invoke();
    }
}