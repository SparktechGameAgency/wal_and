//using UnityEngine;
//using UnityEngine.UI;

///// <summary>
///// AREA FORGE - HorseController
/////
///// Attach to the HorsePrefab.
///// Plays idle animation using the UI Image component.
/////
///// HorsePrefab:
/////   HorsePrefab  ← HorseController + Image (the horse sprite)
///// </summary>
//[RequireComponent(typeof(Image))]
//public class HorseController : MonoBehaviour
//{
//    private Image _image;
//    private HorseData _data;
//    private float _timer = 0f;
//    private int _frame = 0;
//    private bool _playing = false;

//    private void Awake()
//    {
//        _image = GetComponent<Image>();
//    }

//    private void Update()
//    {
//        if (!_playing || _data == null) return;
//        if (_data.idleSprites == null || _data.idleSprites.Length <= 1) return;

//        _timer += Time.deltaTime;
//        if (_timer < 1f / _data.idleFPS) return;

//        _timer = 0f;
//        _frame = (_frame + 1) % _data.idleSprites.Length;
//        _image.sprite = _data.idleSprites[_frame];
//    }

//    /// <summary>
//    /// Call this right after spawning to inject the horse data.
//    /// Immediately shows the first frame and starts the idle loop.
//    /// </summary>
//    public void Setup(HorseData data)
//    {
//        _data = data;
//        _frame = 0;
//        _timer = 0f;
//        _playing = true;

//        if (data.idleSprites != null && data.idleSprites.Length > 0)
//        {
//            _image.sprite = data.idleSprites[0];
//            _image.enabled = true;
//            _image.SetNativeSize();
//        }
//    }

//    public void StopAnimation() => _playing = false;
//    public void PlayAnimation() => _playing = true;
//    public HorseData Data => _data;
//}

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to the HorsePrefab (requires an Image component).
///
/// Animation states:
///   Idle  — loops idleSprites continuously.
///   Walk  — plays walkSprites for N full cycles, then automatically
///            switches back to Idle. If walkCyclesBeforeIdle == 0 it
///            loops walk forever.
/// </summary>
[RequireComponent(typeof(Image))]
public class HorseController : MonoBehaviour
{
    public enum AnimState { Idle, Walk }

    private Image _image;
    private HorseData _data;
    private AnimState _state = AnimState.Idle;
    private float _timer = 0f;
    private int _frame = 0;
    private bool _playing = false;
    private int _walkCyclesCompleted = 0;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake() => _image = GetComponent<Image>();

    private void Update()
    {
        if (!_playing || _data == null) return;

        Sprite[] sprites = CurrentSprites();
        float fps = CurrentFPS();

        if (sprites == null || sprites.Length <= 1) return;

        _timer += Time.deltaTime;
        if (_timer < 1f / fps) return;
        _timer = 0f;

        _frame++;

        if (_frame >= sprites.Length)
        {
            _frame = 0;

            if (_state == AnimState.Walk)
            {
                _walkCyclesCompleted++;

                if (_data.walkCyclesBeforeIdle > 0 &&
                    _walkCyclesCompleted >= _data.walkCyclesBeforeIdle)
                {
                    SwitchToIdle();
                    return;
                }
            }
        }

        _image.sprite = sprites[_frame];
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Call after spawning for a slot horse. Starts idle animation.</summary>
    public void Setup(HorseData data)
    {
        _data = data;
        _state = AnimState.Idle;
        ResetFrameState();
        ShowFirstFrame();
    }

    /// <summary>
    /// Call when the horse is dropped onto the HorseWalkZone.
    /// Plays walk animation, then automatically switches to idle.
    /// </summary>
    public void SetupWalk(HorseData data)
    {
        _data = data;
        _state = AnimState.Walk;
        _walkCyclesCompleted = 0;
        ResetFrameState();

        // Fall back to idle sprites if no walk sprites are assigned
        Sprite[] sprites = (data.walkSprites != null && data.walkSprites.Length > 0)
            ? data.walkSprites
            : data.idleSprites;

        if (sprites != null && sprites.Length > 0)
        {
            _image.sprite = sprites[0];
            _image.enabled = true;
        }

        _playing = true;
    }

    public void StopAnimation() => _playing = false;
    public void PlayAnimation() => _playing = true;
    public HorseData Data => _data;
    public AnimState CurrentState => _state;

    // ── Private helpers ───────────────────────────────────────────────────────

    private Sprite[] CurrentSprites()
    {
        if (_state == AnimState.Walk &&
            _data.walkSprites != null &&
            _data.walkSprites.Length > 0)
            return _data.walkSprites;

        return _data.idleSprites;
    }

    private float CurrentFPS() =>
        (_state == AnimState.Walk &&
         _data.walkSprites != null &&
         _data.walkSprites.Length > 0)
            ? _data.walkFPS
            : _data.idleFPS;

    private void SwitchToIdle()
    {
        _state = AnimState.Idle;
        ResetFrameState();

        Sprite[] idle = _data.idleSprites;
        if (idle != null && idle.Length > 0)
            _image.sprite = idle[0];
    }

    private void ResetFrameState()
    {
        _frame = 0;
        _timer = 0f;
        _playing = true;
    }

    private void ShowFirstFrame()
    {
        Sprite[] sprites = CurrentSprites();
        if (sprites != null && sprites.Length > 0)
        {
            _image.sprite = sprites[0];
            _image.enabled = true;
        }
    }
}