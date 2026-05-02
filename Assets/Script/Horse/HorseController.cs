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

[RequireComponent(typeof(Image))]
public class HorseController : MonoBehaviour
{
    private Image _image;
    private HorseData _data;
    private float _timer = 0f;
    private int _frame = 0;
    private bool _playing = false;

    private void Awake() => _image = GetComponent<Image>();

    private void Update()
    {
        if (!_playing || _data?.idleSprites == null || _data.idleSprites.Length <= 1) return;
        _timer += Time.deltaTime;
        if (_timer < 1f / _data.idleFPS) return;
        _timer = 0f;
        _frame = (_frame + 1) % _data.idleSprites.Length;
        _image.sprite = _data.idleSprites[_frame];
    }

    /// <summary>Call after spawning. Keeps the prefab's original RectTransform size.</summary>
    public void Setup(HorseData data)
    {
        _data = data;
        _frame = 0;
        _timer = 0f;
        _playing = true;

        if (data.idleSprites != null && data.idleSprites.Length > 0)
        {
            _image.sprite = data.idleSprites[0];
            _image.enabled = true;
            // ── FIX: do NOT call SetNativeSize — it blows the sprite to raw pixel
            // dimensions (e.g. 600×500). The prefab's RectTransform size is used as-is.
        }
    }

    public void StopAnimation() => _playing = false;
    public void PlayAnimation() => _playing = true;
    public HorseData Data => _data;
}