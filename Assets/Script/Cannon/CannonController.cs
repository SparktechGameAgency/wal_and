using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CANNON PANEL — CannonController
///
/// Attach to the cannon prefab that gets instantiated inside CannonSlot.
/// Plays idle sprite animation through the UI Image component.
/// Do NOT call SetNativeSize — prefab's own RectTransform size is used.
/// </summary>
[RequireComponent(typeof(Image))]
public class CannonController : MonoBehaviour
{
    private Image _image;
    private CannonData _data;
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

    /// <summary>Call immediately after instantiating the prefab.</summary>
    public void Setup(CannonData data)
    {
        _data = data;
        _frame = 0;
        _timer = 0f;
        _playing = true;

        if (data.idleSprites != null && data.idleSprites.Length > 0)
        {
            _image.sprite = data.idleSprites[0];
            _image.enabled = true;
        }
    }

    public void StopAnimation() => _playing = false;
    public void PlayAnimation() => _playing = true;
    public CannonData Data => _data;
}