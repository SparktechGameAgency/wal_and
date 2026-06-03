//using System;
//using System.Collections;
//using UnityEngine;
//using UnityEngine.UI;

//public class SpriteAnimator : MonoBehaviour
//{
//    [Header("Frames")]
//    public Sprite[] frames;
//    public float fps = 12f;
//    public bool loop = false;

//    [Header("Spawn Frame")]
//    [Tooltip("Projectile spawns on this frame index (0 = first frame)")]
//    public int spawnOnFrame = 3;

//    private Image image;
//    private Coroutine playRoutine;

//    public Action onSpawnFrame;
//    public Action onComplete;

//    void Awake()
//    {
//        // Try this GameObject first, then search children
//        image = GetComponent<Image>();
//        if (image == null)
//            image = GetComponentInChildren<Image>();

//        if (image == null)
//        {
//            Debug.LogError($"[SpriteAnimator] No Image component found on '{gameObject.name}' " +
//                           "or its children. Animation cannot play.");
//            return;
//        }

//        // Clamp spawnOnFrame so it always falls inside the frames array
//        if (frames != null && frames.Length > 0)
//        {
//            spawnOnFrame = Mathf.Clamp(spawnOnFrame, 0, frames.Length - 1);
//            image.sprite = frames[0];
//        }
//    }

//    public void Play()
//    {
//        if (image == null)
//        {
//            Debug.LogError($"[SpriteAnimator] Cannot play — Image is null on '{gameObject.name}'.");
//            onComplete?.Invoke();   // unlock isFiring so the button still works
//            return;
//        }

//        if (frames == null || frames.Length == 0)
//        {
//            Debug.LogError($"[SpriteAnimator] Cannot play — No frames assigned on '{gameObject.name}'.");
//            onComplete?.Invoke();
//            return;
//        }

//        if (playRoutine != null)
//            StopCoroutine(playRoutine);

//        playRoutine = StartCoroutine(Animate());
//    }

//    public void Stop()
//    {
//        if (playRoutine != null)
//        {
//            StopCoroutine(playRoutine);
//            playRoutine = null;
//        }

//        if (image != null && frames != null && frames.Length > 0)
//            image.sprite = frames[0];

//        // Do NOT fire onComplete here. Stop() is called during state
//        // transitions and must not trigger the previous state's callback.
//        // Clear callbacks so they cannot fire late.
//        onSpawnFrame = null;
//        onComplete = null;
//    }

//    public float GetDuration()
//    {
//        if (frames == null || frames.Length == 0 || fps <= 0) return 0f;
//        return frames.Length / fps;
//    }

//    IEnumerator Animate()
//    {
//        float delay = 1f / fps;

//        // Clamp again at runtime in case frames array was changed after Awake
//        int safeSpawnFrame = Mathf.Clamp(spawnOnFrame, 0, frames.Length - 1);

//        do
//        {
//            for (int i = 0; i < frames.Length; i++)
//            {
//                if (frames[i] == null)
//                {
//                    Debug.LogWarning($"[SpriteAnimator] Frame {i} is null on '{gameObject.name}'. Skipping.");
//                    yield return new WaitForSeconds(delay);
//                    continue;
//                }

//                image.sprite = frames[i];

//                if (i == safeSpawnFrame)
//                    onSpawnFrame?.Invoke();

//                yield return new WaitForSeconds(delay);
//            }
//        }
//        while (loop);

//        onComplete?.Invoke();
//        playRoutine = null;
//    }
//    /// <summary>
//    /// Hard-resets the animator without firing callbacks.
//    /// Use when the cannon is being destroyed so stale delegates are not invoked.
//    /// </summary>
//    public void ForceReset()
//    {
//        if (playRoutine != null) { StopCoroutine(playRoutine); playRoutine = null; }
//        onSpawnFrame = null;
//        onComplete = null;
//        if (image != null && frames != null && frames.Length > 0)
//            image.sprite = frames[0];
//    }

//}

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
        // Try this GameObject first, then search children
        image = GetComponent<Image>();
        if (image == null)
            image = GetComponentInChildren<Image>();

        if (image == null)
        {
            Debug.LogError($"[SpriteAnimator] No Image component found on '{gameObject.name}' " +
                           "or its children. Animation cannot play.");
            return;
        }

        // Clamp spawnOnFrame so it always falls inside the frames array
        if (frames != null && frames.Length > 0)
        {
            spawnOnFrame = Mathf.Clamp(spawnOnFrame, 0, frames.Length - 1);
            image.sprite = frames[0];
        }
    }

    public void Play()
    {
        if (image == null)
        {
            Debug.LogError($"[SpriteAnimator] Cannot play — Image is null on '{gameObject.name}'.");
            onComplete?.Invoke();   // unlock isFiring so the button still works
            return;
        }

        if (frames == null || frames.Length == 0)
        {
            Debug.LogError($"[SpriteAnimator] Cannot play — No frames assigned on '{gameObject.name}'.");
            onComplete?.Invoke();
            return;
        }

        if (playRoutine != null)
            StopCoroutine(playRoutine);

        playRoutine = StartCoroutine(Animate());
    }

    public void Stop()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        if (image != null && frames != null && frames.Length > 0)
            image.sprite = frames[0];

        // Do NOT fire onComplete here. Stop() is called during state
        // transitions and must not trigger the previous state's callback.
        // Clear callbacks so they cannot fire late.
        onSpawnFrame = null;
        onComplete = null;
    }

    public float GetDuration()
    {
        if (frames == null || frames.Length == 0 || fps <= 0) return 0f;
        return frames.Length / fps;
    }

    IEnumerator Animate()
    {
        float delay = 1f / fps;

        // Clamp again at runtime in case frames array was changed after Awake
        int safeSpawnFrame = Mathf.Clamp(spawnOnFrame, 0, frames.Length - 1);

        do
        {
            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i] == null)
                {
                    Debug.LogWarning($"[SpriteAnimator] Frame {i} is null on '{gameObject.name}'. Skipping.");
                    yield return new WaitForSeconds(delay);
                    continue;
                }

                image.sprite = frames[i];

                if (i == safeSpawnFrame)
                    onSpawnFrame?.Invoke();

                yield return new WaitForSeconds(delay);
            }
        }
        while (loop);

        onComplete?.Invoke();
        playRoutine = null;
    }
    /// <summary>
    /// Hard-resets the animator without firing callbacks.
    /// Use when the cannon is being destroyed so stale delegates are not invoked.
    /// </summary>
    public void ForceReset()
    {
        if (playRoutine != null) { StopCoroutine(playRoutine); playRoutine = null; }
        onSpawnFrame = null;
        onComplete = null;
        if (image != null && frames != null && frames.Length > 0)
            image.sprite = frames[0];
    }

}