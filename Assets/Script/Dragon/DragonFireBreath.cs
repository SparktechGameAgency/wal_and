//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;

///// <summary>
///// DRAGON FIRE BREATH — UI-native implementation
/////
///// Uses pooled UI Image RectTransforms spawned inside the Canvas so they
///// render correctly in Screen Space - Overlay (ParticleSystem cannot).
/////
///// ════════════════════════════════════════════════════════════════════
/////  SETUP  (3 steps)
///// ════════════════════════════════════════════════════════════════════
/////
/////  1. Add this component to the dragon root (same GameObject as
/////     DragonController).
/////
/////  2. In the dragon prefab, create an EMPTY child called "FirePoint":
/////
/////       DragonRide  (DragonController + DragonFireBreath)
/////         ├── DragonBody
/////         ├── DragonWing
/////         ├── RiderSeat
/////         └── FirePoint        ← NEW empty GameObject
/////
/////     In the Scene view, drag FirePoint so its gizmo (the small axis
/////     cross) sits exactly at the dragon's snout/mouth. You can see
/////     the exact pixel position in the RectTransform Inspector.
/////     That position is used as the fire emission origin every frame.
/////
/////  3. Drag the FirePoint child into the "Fire Point" slot on this
/////     component in the Inspector.
/////     If you leave it blank the script falls back to the dragon's
/////     own pivot (centre) so it still works while you set things up.
/////
///// ════════════════════════════════════════════════════════════════════
/////  WHY A CHILD TRANSFORM INSTEAD OF OFFSET NUMBERS
///// ════════════════════════════════════════════════════════════════════
/////
/////  The previous version used mouthOffsetX / mouthOffsetY numbers you
/////  had to guess. A child RectTransform lets you drag the point
/////  visually in the Scene view — you see exactly where the fire will
/////  come from, and it automatically mirrors when the dragon flips
/////  (because it is a child of the dragon root, so its world position
/////  follows the parent's localScale.x flip).
/////
///// ════════════════════════════════════════════════════════════════════
/////  WIRING IN DragonController
///// ════════════════════════════════════════════════════════════════════
/////  DragonController already calls fireBreath.Play() / .Stop().
/////  Nothing else to change there.
///// </summary>
//public class DragonFireBreath : MonoBehaviour
//{
//    // ── Inspector ──────────────────────────────────────────────────────────────

//    [Header("References")]
//    [Tooltip("Root Canvas. Auto-found via GetComponentInParent if blank.")]
//    [SerializeField] private Canvas rootCanvas;

//    [Tooltip("Empty child GameObject placed at the dragon's snout in the Scene view. " +
//             "Fire particles spawn from this point. " +
//             "Falls back to dragon pivot if not assigned.")]
//    [SerializeField] private RectTransform firePoint;

//    [Tooltip("Sprite used for each fire particle. " +
//             "A soft radial circle is generated procedurally if blank — " +
//             "assign a real fire/smoke sprite for better looks.")]
//    [SerializeField] private Sprite fireSprite;

//    [Header("Emission")]
//    [Tooltip("Particles per second while breathing fire.")]
//    [SerializeField] private float emissionRate = 55f;

//    [Header("Particle behaviour")]
//    [SerializeField] private float lifetimeMin = 0.25f;
//    [SerializeField] private float lifetimeMax = 0.45f;
//    [SerializeField] private float speedMin = 180f;
//    [SerializeField] private float speedMax = 320f;
//    [Tooltip("Half-angle of the emission cone in degrees (0 = straight line, 20 = wide spray).")]
//    [SerializeField] private float coneHalfAngle = 18f;
//    [SerializeField] private float sizeStart = 38f;
//    [SerializeField] private float sizeEnd = 10f;

//    [Header("Colors  tip → base")]
//    [SerializeField] private Color colorTip = new Color(1f, 0.92f, 0.2f);   // bright yellow
//    [SerializeField] private Color colorMid = new Color(1f, 0.35f, 0.05f);  // orange
//    [SerializeField] private Color colorBase = new Color(0.6f, 0.05f, 0f);    // dark red

//    [Header("Pool")]
//    [SerializeField] private int poolSize = 80;

//    // ── Public ─────────────────────────────────────────────────────────────────

//    public bool IsPlaying { get; private set; }

//    // ── Private ────────────────────────────────────────────────────────────────

//    private RectTransform _canvasRt;
//    private GameObject _poolParent;

//    private readonly List<FireParticle> _pool = new List<FireParticle>();
//    private readonly List<FireParticle> _active = new List<FireParticle>();

//    private float _emitAccum;

//    private class FireParticle
//    {
//        public RectTransform rt;
//        public Image img;
//        public Vector2 velocity;
//        public float lifetime;
//        public float age;
//        public float startSize;
//        public Color startColor;
//        public Color endColor;
//    }

//    // ════════════════════════════════════════════════════════════════════════
//    // LIFECYCLE
//    // ════════════════════════════════════════════════════════════════════════

//    private void Awake()
//    {
//        if (rootCanvas == null)
//            rootCanvas = GetComponentInParent<Canvas>();

//        if (rootCanvas == null)
//        {
//            Debug.LogError("[DragonFireBreath] No Canvas found. Assign Root Canvas in Inspector.", this);
//            enabled = false;
//            return;
//        }

//        _canvasRt = rootCanvas.GetComponent<RectTransform>();

//        // Auto-find FirePoint by name if not assigned in Inspector
//        if (firePoint == null)
//        {
//            var found = transform.Find("FirePoint");
//            if (found != null)
//            {
//                firePoint = found.GetComponent<RectTransform>();
//                Debug.Log("[DragonFireBreath] Auto-found child 'FirePoint'.", this);
//            }
//            else
//            {
//                Debug.LogWarning("[DragonFireBreath] No FirePoint assigned or found. " +
//                                 "Fire will spawn from the dragon's pivot. " +
//                                 "Create an empty child named 'FirePoint' and position it at the snout.", this);
//            }
//        }

//        // Invisible container for all pooled particles — always on top of canvas
//        _poolParent = new GameObject("DragonFireParticles");
//        _poolParent.transform.SetParent(rootCanvas.transform, false);
//        var poolRt = _poolParent.AddComponent<RectTransform>();
//        poolRt.anchorMin = Vector2.zero;
//        poolRt.anchorMax = Vector2.one;
//        poolRt.offsetMin = Vector2.zero;
//        poolRt.offsetMax = Vector2.zero;
//        _poolParent.transform.SetAsLastSibling();

//        BuildPool();
//    }

//    private void Update()
//    {
//        if (IsPlaying) EmitThisFrame();
//        TickParticles();
//    }

//    private void OnDestroy()
//    {
//        if (_poolParent != null)
//            Destroy(_poolParent);
//    }

//    // ════════════════════════════════════════════════════════════════════════
//    // PUBLIC API
//    // ════════════════════════════════════════════════════════════════════════

//    public void Play()
//    {
//        IsPlaying = true;
//        _emitAccum = 0f;
//        if (_poolParent != null) _poolParent.SetActive(true);
//    }

//    public void Stop()
//    {
//        IsPlaying = false;
//        // Active particles finish their lifetime naturally.
//    }

//    public void StopImmediate()
//    {
//        IsPlaying = false;
//        for (int i = _active.Count - 1; i >= 0; i--)
//            ReturnToPool(_active[i]);
//        _active.Clear();
//    }

//    // ════════════════════════════════════════════════════════════════════════
//    // EMISSION
//    // ════════════════════════════════════════════════════════════════════════

//    private void EmitThisFrame()
//    {
//        _emitAccum += emissionRate * Time.deltaTime;
//        int count = Mathf.FloorToInt(_emitAccum);
//        _emitAccum -= count;
//        for (int i = 0; i < count; i++) SpawnParticle();
//    }

//    private void SpawnParticle()
//    {
//        FireParticle p = GetFromPool();
//        if (p == null) return;

//        // ── Spawn position ────────────────────────────────────────────────────
//        // Use FirePoint's world position if available; fall back to dragon pivot.
//        // Because FirePoint is a CHILD of the dragon root, Unity already moves
//        // it to the correct mirrored side when localScale.x flips — no manual
//        // offset math needed here.
//        Transform origin = (firePoint != null) ? (Transform)firePoint : transform;
//        Vector3 worldPos = origin.position;
//        p.rt.anchoredPosition = WorldToCanvasAnchoredPos(worldPos);
//        p.rt.SetAsLastSibling();

//        // ── Emission direction ────────────────────────────────────────────────
//        // Read localScale.x to know which way the dragon is currently facing.
//        // DragonController sets scale.x negative when flipped (facing right when
//        // the sprite default faces left, or vice-versa).
//        // We determine the world-space "forward" direction of the snout:
//        //   · transform.right  gives the local +X axis in world space
//        //   · When scale.x < 0 the sprite is mirrored so local +X points the
//        //     other way — transform.right already accounts for that.
//        // Therefore transform.right is always the "out of the mouth" vector.
//        Vector2 forward = transform.right;   // world-space mouth direction

//        float spread = Random.Range(-coneHalfAngle, coneHalfAngle) * Mathf.Deg2Rad;
//        float cos = Mathf.Cos(spread), sin = Mathf.Sin(spread);
//        // Rotate forward by spread angle
//        Vector2 dir = new Vector2(
//            forward.x * cos - forward.y * sin,
//            forward.x * sin + forward.y * cos);

//        p.velocity = dir * Random.Range(speedMin, speedMax);

//        // ── Lifetime & looks ──────────────────────────────────────────────────
//        p.lifetime = Random.Range(lifetimeMin, lifetimeMax);
//        p.age = 0f;
//        p.startSize = sizeStart * Random.Range(0.75f, 1.25f);
//        p.startColor = Color.Lerp(colorTip, colorMid, Random.Range(0f, 0.5f));
//        p.endColor = colorBase;

//        p.img.color = p.startColor;
//        p.rt.sizeDelta = Vector2.one * p.startSize;
//        p.rt.gameObject.SetActive(true);

//        _active.Add(p);
//    }

//    // ════════════════════════════════════════════════════════════════════════
//    // TICK
//    // ════════════════════════════════════════════════════════════════════════

//    private void TickParticles()
//    {
//        float dt = Time.deltaTime;

//        for (int i = _active.Count - 1; i >= 0; i--)
//        {
//            FireParticle p = _active[i];
//            p.age += dt;
//            float t = Mathf.Clamp01(p.age / p.lifetime);

//            // Move
//            p.rt.anchoredPosition += p.velocity * dt;

//            // Drag
//            p.velocity *= (1f - dt * 3.5f);

//            // Hot air rises
//            p.velocity.y += 30f * dt;

//            // Size
//            p.rt.sizeDelta = Vector2.one * Mathf.Lerp(p.startSize, sizeEnd, t);

//            // Color + fade
//            Color col = Color.Lerp(p.startColor, p.endColor, t);
//            col.a = 1f - t;
//            p.img.color = col;

//            if (p.age >= p.lifetime)
//            {
//                ReturnToPool(p);
//                _active.RemoveAt(i);
//            }
//        }
//    }

//    // ════════════════════════════════════════════════════════════════════════
//    // POOL
//    // ════════════════════════════════════════════════════════════════════════

//    private void BuildPool()
//    {
//        Sprite spr = fireSprite != null ? fireSprite : MakeCircleSprite();

//        for (int i = 0; i < poolSize; i++)
//        {
//            var go = new GameObject("FP");
//            go.SetActive(false);
//            go.transform.SetParent(_poolParent.transform, false);

//            var rt = go.AddComponent<RectTransform>();
//            rt.anchorMin = new Vector2(0.5f, 0.5f);
//            rt.anchorMax = new Vector2(0.5f, 0.5f);
//            rt.pivot = new Vector2(0.5f, 0.5f);

//            var img = go.AddComponent<Image>();
//            img.sprite = spr;
//            img.raycastTarget = false;

//            _pool.Add(new FireParticle { rt = rt, img = img });
//        }
//    }

//    private FireParticle GetFromPool()
//    {
//        if (_pool.Count == 0) return null;
//        var p = _pool[_pool.Count - 1];
//        _pool.RemoveAt(_pool.Count - 1);
//        return p;
//    }

//    private void ReturnToPool(FireParticle p)
//    {
//        p.rt.gameObject.SetActive(false);
//        _pool.Add(p);
//    }

//    // ════════════════════════════════════════════════════════════════════════
//    // HELPERS
//    // ════════════════════════════════════════════════════════════════════════

//    private Vector2 WorldToCanvasAnchoredPos(Vector3 worldPos)
//    {
//        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, worldPos);
//        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//            _canvasRt, screenPos, null, out Vector2 local);
//        return local;
//    }

//    private static Sprite MakeCircleSprite()
//    {
//        const int size = 32;
//        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
//        tex.filterMode = FilterMode.Bilinear;
//        float half = size * 0.5f;
//        for (int y = 0; y < size; y++)
//            for (int x = 0; x < size; x++)
//            {
//                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(half, half));
//                float a = Mathf.Clamp01(1f - d / half);
//                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
//            }
//        tex.Apply();
//        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
//    }

//#if UNITY_EDITOR
//    // Draw a small orange circle gizmo at the FirePoint position in Scene view
//    // so you can see the spawn origin without entering Play mode.
//    private void OnDrawGizmosSelected()
//    {
//        Transform origin = (firePoint != null) ? (Transform)firePoint : transform;
//        UnityEditor.Handles.color = new Color(1f, 0.4f, 0f, 0.9f);
//        UnityEditor.Handles.DrawSolidDisc(origin.position, Vector3.back, 6f);

//        // Draw an arrow showing the current fire direction
//        Vector3 dir = transform.right * 40f;
//        Gizmos.color = new Color(1f, 0.8f, 0f);
//        Gizmos.DrawRay(origin.position, dir);
//    }
//#endif
//}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// DRAGON FIRE BREATH — UI-native implementation
///
/// Uses pooled UI Image RectTransforms spawned inside the Canvas so they
/// render correctly in Screen Space - Overlay (ParticleSystem cannot).
///
/// ════════════════════════════════════════════════════════════════════
///  SETUP  (3 steps)
/// ════════════════════════════════════════════════════════════════════
///
///  1. Add this component to the dragon root (same GameObject as
///     DragonController).
///
///  2. In the dragon prefab, create an EMPTY child called "FirePoint":
///
///       DragonRide  (DragonController + DragonFireBreath)
///         ├── DragonBody
///         ├── DragonWing
///         ├── RiderSeat
///         └── FirePoint        ← NEW empty GameObject
///
///     In the Scene view, drag FirePoint so its gizmo (the small axis
///     cross) sits exactly at the dragon's snout/mouth. You can see
///     the exact pixel position in the RectTransform Inspector.
///     That position is used as the fire emission origin every frame.
///
///  3. Drag the FirePoint child into the "Fire Point" slot on this
///     component in the Inspector.
///     If you leave it blank the script falls back to the dragon's
///     own pivot (centre) so it still works while you set things up.
///
/// ════════════════════════════════════════════════════════════════════
///  WHY A CHILD TRANSFORM INSTEAD OF OFFSET NUMBERS
/// ════════════════════════════════════════════════════════════════════
///
///  The previous version used mouthOffsetX / mouthOffsetY numbers you
///  had to guess. A child RectTransform lets you drag the point
///  visually in the Scene view — you see exactly where the fire will
///  come from, and it automatically mirrors when the dragon flips
///  (because it is a child of the dragon root, so its world position
///  follows the parent's localScale.x flip).
///
/// ════════════════════════════════════════════════════════════════════
///  WIRING IN DragonController
/// ════════════════════════════════════════════════════════════════════
///  DragonController already calls fireBreath.Play() / .Stop().
///  Nothing else to change there.
/// </summary>
public class DragonFireBreath : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────────

    [Header("References")]
    [Tooltip("Root Canvas. Auto-found via GetComponentInParent if blank.")]
    [SerializeField] private Canvas rootCanvas;

    [Tooltip("Empty child GameObject placed at the dragon's snout in the Scene view. " +
             "Fire particles spawn from this point. " +
             "Falls back to dragon pivot if not assigned.")]
    [SerializeField] private RectTransform firePoint;

    [Tooltip("Sprite used for each fire particle. " +
             "A soft radial circle is generated procedurally if blank — " +
             "assign a real fire/smoke sprite for better looks.")]
    [SerializeField] private Sprite fireSprite;

    [Header("Emission")]
    [Tooltip("Particles per second while breathing fire.")]
    [SerializeField] private float emissionRate = 55f;

    [Header("Particle behaviour")]
    [SerializeField] private float lifetimeMin = 0.25f;
    [SerializeField] private float lifetimeMax = 0.45f;
    [SerializeField] private float speedMin = 180f;
    [SerializeField] private float speedMax = 320f;
    [Tooltip("Half-angle of the emission cone in degrees (0 = straight line, 20 = wide spray).")]
    [SerializeField] private float coneHalfAngle = 18f;
    [SerializeField] private float sizeStart = 38f;
    [SerializeField] private float sizeEnd = 10f;

    [Header("Colors  tip → base")]
    [SerializeField] private Color colorTip = new Color(1f, 0.92f, 0.2f);   // bright yellow
    [SerializeField] private Color colorMid = new Color(1f, 0.35f, 0.05f);  // orange
    [SerializeField] private Color colorBase = new Color(0.6f, 0.05f, 0f);    // dark red

    [Header("Pool")]
    [SerializeField] private int poolSize = 80;

    // ── Public ─────────────────────────────────────────────────────────────────

    public bool IsPlaying { get; private set; }

    // Set by DragonController each frame while attacking so fire aims at the enemy.
    // When null, fire shoots horizontally in the direction the dragon faces.
    private Transform _aimTarget;
    public void SetTarget(Transform t) { _aimTarget = t; }
    public void ClearTarget() { _aimTarget = null; }

    // ── Private ────────────────────────────────────────────────────────────────

    private RectTransform _canvasRt;
    private GameObject _poolParent;

    private readonly List<FireParticle> _pool = new List<FireParticle>();
    private readonly List<FireParticle> _active = new List<FireParticle>();

    private float _emitAccum;

    private class FireParticle
    {
        public RectTransform rt;
        public Image img;
        public Vector2 velocity;
        public float lifetime;
        public float age;
        public float startSize;
        public Color startColor;
        public Color endColor;
    }

    // ════════════════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();

        if (rootCanvas == null)
        {
            Debug.LogError("[DragonFireBreath] No Canvas found. Assign Root Canvas in Inspector.", this);
            enabled = false;
            return;
        }

        _canvasRt = rootCanvas.GetComponent<RectTransform>();

        // Auto-find FirePoint by name if not assigned in Inspector
        if (firePoint == null)
        {
            var found = transform.Find("FirePoint");
            if (found != null)
            {
                firePoint = found.GetComponent<RectTransform>();
                Debug.Log("[DragonFireBreath] Auto-found child 'FirePoint'.", this);
            }
            else
            {
                Debug.LogWarning("[DragonFireBreath] No FirePoint assigned or found. " +
                                 "Fire will spawn from the dragon's pivot. " +
                                 "Create an empty child named 'FirePoint' and position it at the snout.", this);
            }
        }

        // Invisible container for all pooled particles — always on top of canvas
        _poolParent = new GameObject("DragonFireParticles");
        _poolParent.transform.SetParent(rootCanvas.transform, false);
        var poolRt = _poolParent.AddComponent<RectTransform>();
        poolRt.anchorMin = Vector2.zero;
        poolRt.anchorMax = Vector2.one;
        poolRt.offsetMin = Vector2.zero;
        poolRt.offsetMax = Vector2.zero;
        _poolParent.transform.SetAsLastSibling();

        BuildPool();
    }

    private void Update()
    {
        if (IsPlaying) EmitThisFrame();
        TickParticles();
    }

    private void OnDestroy()
    {
        if (_poolParent != null)
            Destroy(_poolParent);
    }

    // ════════════════════════════════════════════════════════════════════════
    // PUBLIC API
    // ════════════════════════════════════════════════════════════════════════

    public void Play()
    {
        IsPlaying = true;
        _emitAccum = 0f;
        if (_poolParent != null) _poolParent.SetActive(true);
    }

    public void Stop()
    {
        IsPlaying = false;
        // Active particles finish their lifetime naturally.
    }

    public void StopImmediate()
    {
        IsPlaying = false;
        for (int i = _active.Count - 1; i >= 0; i--)
            ReturnToPool(_active[i]);
        _active.Clear();
    }

    // ════════════════════════════════════════════════════════════════════════
    // EMISSION
    // ════════════════════════════════════════════════════════════════════════

    private void EmitThisFrame()
    {
        _emitAccum += emissionRate * Time.deltaTime;
        int count = Mathf.FloorToInt(_emitAccum);
        _emitAccum -= count;
        for (int i = 0; i < count; i++) SpawnParticle();
    }

    private void SpawnParticle()
    {
        FireParticle p = GetFromPool();
        if (p == null) return;

        // ── Spawn position ────────────────────────────────────────────────────
        // Use FirePoint's world position if available; fall back to dragon pivot.
        // Because FirePoint is a CHILD of the dragon root, Unity already moves
        // it to the correct mirrored side when localScale.x flips — no manual
        // offset math needed here.
        Transform origin = (firePoint != null) ? (Transform)firePoint : transform;
        Vector3 worldPos = origin.position;
        p.rt.anchoredPosition = WorldToCanvasAnchoredPos(worldPos);
        p.rt.SetAsLastSibling();

        // ── Emission direction ────────────────────────────────────────────────
        // If we have a target, aim directly at it from the FirePoint.
        // This makes fire reach enemies that are above/below the dragon.
        // When no target, fall back to transform.right (horizontal facing).
        Vector2 forward;
        if (_aimTarget != null)
        {
            Vector2 toTarget = (Vector2)_aimTarget.position - (Vector2)origin.position;
            forward = toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : (Vector2)transform.right;
        }
        else
        {
            forward = transform.right;
        }

        float spread = Random.Range(-coneHalfAngle, coneHalfAngle) * Mathf.Deg2Rad;
        float cos = Mathf.Cos(spread), sin = Mathf.Sin(spread);
        Vector2 dir = new Vector2(
            forward.x * cos - forward.y * sin,
            forward.x * sin + forward.y * cos);

        p.velocity = dir * Random.Range(speedMin, speedMax);

        // ── Lifetime & looks ──────────────────────────────────────────────────
        p.lifetime = Random.Range(lifetimeMin, lifetimeMax);
        p.age = 0f;
        p.startSize = sizeStart * Random.Range(0.75f, 1.25f);
        p.startColor = Color.Lerp(colorTip, colorMid, Random.Range(0f, 0.5f));
        p.endColor = colorBase;

        p.img.color = p.startColor;
        p.rt.sizeDelta = Vector2.one * p.startSize;
        p.rt.gameObject.SetActive(true);

        _active.Add(p);
    }

    // ════════════════════════════════════════════════════════════════════════
    // TICK
    // ════════════════════════════════════════════════════════════════════════

    private void TickParticles()
    {
        float dt = Time.deltaTime;

        for (int i = _active.Count - 1; i >= 0; i--)
        {
            FireParticle p = _active[i];
            p.age += dt;
            float t = Mathf.Clamp01(p.age / p.lifetime);

            // Move
            p.rt.anchoredPosition += p.velocity * dt;

            // Drag
            p.velocity *= (1f - dt * 3.5f);

            // Hot air rises
            p.velocity.y += 30f * dt;

            // Size
            p.rt.sizeDelta = Vector2.one * Mathf.Lerp(p.startSize, sizeEnd, t);

            // Color + fade
            // Stay fully opaque for the first 80% of life, then fade out
            // quickly in the last 20%. This keeps fire visible across the
            // full travel distance and only vanishes right at the tip.
            Color col = Color.Lerp(p.startColor, p.endColor, t);
            float fadeT = Mathf.Clamp01((t - 0.8f) / 0.2f);
            col.a = 1f - fadeT;
            p.img.color = col;

            if (p.age >= p.lifetime)
            {
                ReturnToPool(p);
                _active.RemoveAt(i);
            }
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // POOL
    // ════════════════════════════════════════════════════════════════════════

    private void BuildPool()
    {
        Sprite spr = fireSprite != null ? fireSprite : MakeCircleSprite();

        for (int i = 0; i < poolSize; i++)
        {
            var go = new GameObject("FP");
            go.SetActive(false);
            go.transform.SetParent(_poolParent.transform, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            var img = go.AddComponent<Image>();
            img.sprite = spr;
            img.raycastTarget = false;

            _pool.Add(new FireParticle { rt = rt, img = img });
        }
    }

    private FireParticle GetFromPool()
    {
        if (_pool.Count == 0) return null;
        var p = _pool[_pool.Count - 1];
        _pool.RemoveAt(_pool.Count - 1);
        return p;
    }

    private void ReturnToPool(FireParticle p)
    {
        p.rt.gameObject.SetActive(false);
        _pool.Add(p);
    }

    // ════════════════════════════════════════════════════════════════════════
    // HELPERS
    // ════════════════════════════════════════════════════════════════════════

    private Vector2 WorldToCanvasAnchoredPos(Vector3 worldPos)
    {
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRt, screenPos, null, out Vector2 local);
        return local;
    }

    private static Sprite MakeCircleSprite()
    {
        const int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(half, half));
                float a = Mathf.Clamp01(1f - d / half);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

#if UNITY_EDITOR
    // Draw a small orange circle gizmo at the FirePoint position in Scene view
    // so you can see the spawn origin without entering Play mode.
    private void OnDrawGizmosSelected()
    {
        Transform origin = (firePoint != null) ? (Transform)firePoint : transform;
        UnityEditor.Handles.color = new Color(1f, 0.4f, 0f, 0.9f);
        UnityEditor.Handles.DrawSolidDisc(origin.position, Vector3.back, 6f);

        // Draw an arrow showing the current fire direction
        Vector3 dir = transform.right * 40f;
        Gizmos.color = new Color(1f, 0.8f, 0f);
        Gizmos.DrawRay(origin.position, dir);
    }
#endif
}