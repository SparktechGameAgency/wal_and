using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// AREA FORGE - ImagePixelator
/// Attach to any GameObject that has a UI Image component.
/// Applies the PixelateImage shader and exposes all variables in the Inspector.
/// Updates live in both Edit Mode and Play Mode — scrub sliders to preview instantly.
///
/// Quick start:
///   1. Add PixelateImage.shader and this script to your project
///   2. Attach ImagePixelator to any UI Image GameObject
///   3. Drag the Pixel Block Size slider in the Inspector
///
/// The script handles material instancing automatically —
/// each Image gets its own private material so changing one never affects others.
/// </summary>
[RequireComponent(typeof(Image))]
[ExecuteAlways]
public class ImagePixelator : MonoBehaviour
{
    // ─── Cached shader property IDs ───────────────────────────────────────────
    private static readonly int ID_PixelSize = Shader.PropertyToID("_PixelSize");
    private static readonly int ID_PixelScaleX = Shader.PropertyToID("_PixelScaleX");
    private static readonly int ID_PixelScaleY = Shader.PropertyToID("_PixelScaleY");
    private static readonly int ID_RectSize = Shader.PropertyToID("_RectSize");
    private static readonly int ID_EnableColorDepth = Shader.PropertyToID("_EnableColorDepth");
    private static readonly int ID_ColorDepth = Shader.PropertyToID("_ColorDepth");
    private static readonly int ID_EnableOutline = Shader.PropertyToID("_EnableOutline");
    private static readonly int ID_OutlineColor = Shader.PropertyToID("_OutlineColor");
    private static readonly int ID_OutlineThickness = Shader.PropertyToID("_OutlineThickness");
    private static readonly int ID_OutlineThreshold = Shader.PropertyToID("_OutlineThreshold");
    private static readonly int ID_Color = Shader.PropertyToID("_Color");

    // ─── Inspector Variables ──────────────────────────────────────────────────

    [Header("── Effect Toggle ──────────────────────")]
    [Tooltip("Disabling this removes the effect and restores the original material.")]
    [SerializeField] private bool enableEffect = true;

    [Header("── Pixelation ──────────────────────────")]
    [Tooltip("Size of each pixel block in screen pixels. 1 = no pixelation. Higher = chunkier.")]
    [Range(1, 256)]
    [SerializeField] private int pixelBlockSize = 16;

    [Tooltip("Stretch blocks horizontally. Values above 1 give a wide-pixel / CRT look.")]
    [Range(0.1f, 4f)]
    [SerializeField] private float horizontalScale = 1f;

    [Tooltip("Stretch blocks vertically. Values above 1 give a tall-pixel look.")]
    [Range(0.1f, 4f)]
    [SerializeField] private float verticalScale = 1f;

    [Header("── Colour Depth ────────────────────────")]
    [Tooltip("Quantise each RGB channel to a fixed number of steps for a retro palette effect.")]
    [SerializeField] private bool crushColorDepth = false;

    [Tooltip("Steps per channel. 2 = 1-bit posterised, 4 = Game Boy, 256 = no change.")]
    [Range(2, 256)]
    [SerializeField] private int colorsPerChannel = 8;

    [Header("── Pixel Outline ───────────────────────")]
    [Tooltip("Draws a hard-edged border around the image at pixel-block boundaries.")]
    [SerializeField] private bool enableOutline = false;

    [SerializeField] private Color outlineColor = Color.black;

    [Tooltip("How many pixel blocks thick the outline is.")]
    [Range(1, 8)]
    [SerializeField] private int outlineThickness = 1;

    [Tooltip("Alpha threshold for edge detection. Lower = thicker, softer outline.")]
    [Range(0.01f, 1f)]
    [SerializeField] private float outlineThreshold = 0.1f;

    [Header("── Tint ────────────────────────────────")]
    [SerializeField] private Color tintColor = Color.white;

    // ─── Private ──────────────────────────────────────────────────────────────

    private Image _image;
    private RectTransform _rectTransform;
    private Material _matInstance;
    private Material _originalMat;
    private Shader _shader;

    private Vector2 _lastRectSize;      // detect resize without polling every frame

    private const string ShaderName = "AreaForge/PixelateImage";

    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

    private void OnEnable()
    {
        _image = GetComponent<Image>();
        _rectTransform = GetComponent<RectTransform>();
        _originalMat = _image.material;

        _shader = Shader.Find(ShaderName);

        if (_shader == null)
        {
            Debug.LogError($"[ImagePixelator] Shader '{ShaderName}' not found. " +
                           "Make sure PixelateImage.shader is in your project.");
            return;
        }

        _matInstance = new Material(_shader) { name = $"PixelateImage_{name}" };
        _image.material = _matInstance;

        PushToShader();
    }

    private void OnDisable()
    {
        // Restore the original material and destroy our instance
        if (_image != null && _originalMat != null)
            _image.material = _originalMat;

        if (_matInstance != null)
        {
            if (Application.isPlaying)
                Destroy(_matInstance);
            else
                DestroyImmediate(_matInstance);

            _matInstance = null;
        }
    }

    private void OnValidate()
    {
        // Fires whenever any Inspector field changes — push immediately
        if (_matInstance != null)
            PushToShader();
    }

    private void Update()
    {
        if (_matInstance == null) return;

        // Re-push if the RectTransform was resized (e.g. responsive layout)
        Vector2 currentSize = _rectTransform.rect.size;
        if (currentSize != _lastRectSize)
        {
            _lastRectSize = currentSize;
            PushToShader();
        }
    }

    // ─── Shader Push ──────────────────────────────────────────────────────────

    /// <summary>
    /// Writes all Inspector values into the material's shader properties.
    /// Called on validate, enable, and when the rect resizes.
    /// </summary>
    private void PushToShader()
    {
        if (_matInstance == null || _rectTransform == null) return;

        // Always send the current rect size so the shader's UV math stays accurate
        Rect rect = _rectTransform.rect;
        _matInstance.SetVector(ID_RectSize, new Vector4(rect.width, rect.height, 0f, 0f));

        if (!enableEffect)
        {
            // Bypass: set block size to 1 (effectively no pixelation)
            _matInstance.SetFloat(ID_PixelSize, 1f);
            _matInstance.SetFloat(ID_PixelScaleX, 1f);
            _matInstance.SetFloat(ID_PixelScaleY, 1f);
            _matInstance.SetFloat(ID_EnableColorDepth, 0f);
            _matInstance.SetFloat(ID_EnableOutline, 0f);
            _matInstance.SetColor(ID_Color, tintColor);
            return;
        }

        // Pixelation
        _matInstance.SetFloat(ID_PixelSize, Mathf.Max(1f, pixelBlockSize));
        _matInstance.SetFloat(ID_PixelScaleX, horizontalScale);
        _matInstance.SetFloat(ID_PixelScaleY, verticalScale);

        // Colour depth
        _matInstance.SetFloat(ID_EnableColorDepth, crushColorDepth ? 1f : 0f);
        _matInstance.SetFloat(ID_ColorDepth, Mathf.Max(2f, colorsPerChannel));

        // Outline
        _matInstance.SetFloat(ID_EnableOutline, enableOutline ? 1f : 0f);
        _matInstance.SetColor(ID_OutlineColor, outlineColor);
        _matInstance.SetFloat(ID_OutlineThickness, Mathf.Max(1f, outlineThickness));
        _matInstance.SetFloat(ID_OutlineThreshold, outlineThreshold);

        // Tint
        _matInstance.SetColor(ID_Color, tintColor);
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>Set the pixel block size and refresh the shader immediately.</summary>
    public void SetPixelSize(int size)
    {
        pixelBlockSize = Mathf.Clamp(size, 1, 256);
        PushToShader();
    }

    /// <summary>Useful for smooth animations — pass a float, rounds internally.</summary>
    public void SetPixelSizeFloat(float size)
    {
        pixelBlockSize = Mathf.RoundToInt(Mathf.Clamp(size, 1f, 256f));
        PushToShader();
    }

    /// <summary>Toggle the effect on or off at runtime.</summary>
    public void SetEffectEnabled(bool active)
    {
        enableEffect = active;
        PushToShader();
    }

    /// <summary>
    /// Animate a pixel-dissolve transition.
    /// Pass normalizedTime 0→1 to go from fully pixelated → crisp.
    /// </summary>
    public void AnimatePixelDissolve(float normalizedTime)
    {
        float size = Mathf.Lerp(64f, 1f, Mathf.Clamp01(normalizedTime));
        SetPixelSizeFloat(size);
    }

    /// <summary>
    /// Example coroutine — call StartCoroutine(PixelDissolveIn(1.5f)) to
    /// animate the image sharpening over 1.5 seconds on spawn.
    /// </summary>
    public System.Collections.IEnumerator PixelDissolveIn(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            AnimatePixelDissolve(t / duration);
            yield return null;
        }
        AnimatePixelDissolve(1f); // ensure we land on exactly crisp
    }
}