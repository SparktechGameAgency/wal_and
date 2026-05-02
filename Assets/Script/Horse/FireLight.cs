using UnityEngine;
using UnityEngine.Rendering.Universal;   // ← requires URP package

/// <summary>
/// FireLight — attach to a child GameObject inside HorseArea.
///
/// Setup:
///   HorseArea
///     └── FireLight          ← this script + Light2D component
///           └── FireSprite   ← (optional) your fire particle / animated sprite
///
/// On the Light2D component set:
///   Light Type  : Point
///   Color       : warm orange  (#FF6A00 or similar)
///   Intensity   : 1.2  (baseIntensity below)
///   Outer Radius: 3.5  (baseRadius below)
///   Target Sorting Layers: include the layer your horse sprite is on
///
/// The script drives Intensity and Outer Radius with layered Perlin noise
/// so the glow breathes and flickers exactly like a real flame.
/// </summary>
[RequireComponent(typeof(Light2D))]
public class FireLight : MonoBehaviour
{
    // ─── Inspector ────────────────────────────────────────────────────────────

    [Header("Base light values — match your Light2D Inspector settings")]
    [SerializeField] private float baseIntensity = 1.2f;
    [SerializeField] private float baseRadius = 3.5f;

    [Header("Flicker — intensity")]
    [Tooltip("How much the intensity can deviate above/below the base value")]
    [SerializeField] private float intensityVariance = 0.35f;
    [Tooltip("Speed of the slow breathing wave")]
    [SerializeField] private float breathSpeed = 1.1f;
    [Tooltip("Speed of the fast flicker wave")]
    [SerializeField] private float flickerSpeed = 7f;
    [Tooltip("How much the fast flicker contributes vs the slow breath (0-1)")]
    [SerializeField][Range(0f, 1f)] private float flickerWeight = 0.4f;

    [Header("Flicker — radius")]
    [SerializeField] private float radiusVariance = 0.4f;
    [SerializeField] private float radiusSpeed = 2.3f;

    [Header("Color pulse (optional warm shimmer)")]
    [Tooltip("Tick to enable a subtle warm-to-cool color shift on each flicker")]
    [SerializeField] private bool colorPulse = true;
    [SerializeField] private Color warmColor = new Color(1.00f, 0.42f, 0.00f); // deep orange
    [SerializeField] private Color coolColor = new Color(1.00f, 0.72f, 0.20f); // bright yellow
    [SerializeField] private float colorSpeed = 3.5f;

    // ─── Private ─────────────────────────────────────────────────────────────

    private Light2D _light;

    // Perlin noise offsets — randomised per instance so two fires won't sync
    private float _offsetBreath;
    private float _offsetFlicker;
    private float _offsetRadius;
    private float _offsetColor;

    // ─── Unity Lifecycle ─────────────────────────────────────────────────────

    private void Awake()
    {
        _light = GetComponent<Light2D>();

        // Random seed offsets so multiple fires in the scene flicker independently
        _offsetBreath = Random.Range(0f, 100f);
        _offsetFlicker = Random.Range(0f, 100f);
        _offsetRadius = Random.Range(0f, 100f);
        _offsetColor = Random.Range(0f, 100f);
    }

    private void Update()
    {
        float t = Time.time;

        // ── Intensity ─────────────────────────────────────────────────────────
        // Two Perlin layers: slow breath + fast flicker, blended by flickerWeight
        float breath = Mathf.PerlinNoise(_offsetBreath + t * breathSpeed, 0f);
        float flicker = Mathf.PerlinNoise(_offsetFlicker + t * flickerSpeed, 0f);
        float blend = Mathf.Lerp(breath, flicker, flickerWeight);           // 0-1
        _light.intensity = baseIntensity + (blend - 0.5f) * 2f * intensityVariance;

        // ── Outer Radius ──────────────────────────────────────────────────────
        float r = Mathf.PerlinNoise(_offsetRadius + t * radiusSpeed, 0f);
        _light.pointLightOuterRadius = baseRadius + (r - 0.5f) * 2f * radiusVariance;

        // ── Color pulse ───────────────────────────────────────────────────────
        if (colorPulse)
        {
            float c = Mathf.PerlinNoise(_offsetColor + t * colorSpeed, 0f);
            _light.color = Color.Lerp(warmColor, coolColor, c);
        }
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>Instantly kills the light (e.g. when a horse leaves the area).</summary>
    public void Extinguish() => _light.enabled = false;

    /// <summary>Re-lights the fire.</summary>
    public void Ignite() => _light.enabled = true;
}