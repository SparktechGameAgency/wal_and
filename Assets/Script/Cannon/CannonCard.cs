////////using UnityEngine;
////////using UnityEngine.UI;
////////using TMPro;

/////////// <summary>
/////////// CANNON PANEL — CannonCard
///////////
/////////// Attach this to the root of your CannonCard prefab.
/////////// All child references are auto-wired by name in Awake() — no Inspector
/////////// drag-and-drop required. Keep these exact child GameObject names:
///////////
///////////   CannonCard  (root — Button + this script)
///////////   ├── CannonImage      (Image component)
///////////   ├── CardName         (TextMeshProUGUI)
///////////   ├── Selected         (any GameObject — active when this card is selected)
///////////   ├── Locked           (any GameObject — active when type hasn't been bought yet)
///////////   └── UpgradeBadge     (GameObject with TextMeshProUGUI — shows "2/3" or "MAX")
///////////
/////////// Card clicks are forwarded to CannonPanelManager.Instance.OnCardSelected(this).
/////////// </summary>
////////[RequireComponent(typeof(Button))]
////////public class CannonCard : MonoBehaviour
////////{
////////    // ── Auto-wired at runtime ──────────────────────────────────────────────────
////////    public Image _cannonImage;
////////    public TextMeshProUGUI _nameText;
////////    public GameObject _selectedHighlight;
////////    public GameObject _lockOverlay;
////////    public GameObject _badgeRoot;
////////    public TextMeshProUGUI _badgeText;
////////    public Button _button;

////////    // ── Runtime data ───────────────────────────────────────────────────────────
////////    private CannonData _data;
////////    private int _inventoryId = -1;
////////    private bool _isBuyMode = true;

////////    public CannonData Data => _data;
////////    public int InventoryId => _inventoryId;
////////    public bool IsBuyMode => _isBuyMode;

////////    // ══════════════════════════════════════════════════════════════════════════
////////    // UNITY
////////    // ══════════════════════════════════════════════════════════════════════════

////////    private void Awake()
////////    {
////////        Transform t = transform;

////////        var imgT = t.Find("CannonImage");
////////        if (imgT != null) _cannonImage = imgT.GetComponent<Image>();

////////        var nameT = t.Find("CardName");
////////        if (nameT != null) _nameText = nameT.GetComponent<TextMeshProUGUI>();

////////        var selT = t.Find("Selected");
////////        if (selT != null) _selectedHighlight = selT.gameObject;

////////        var lockT = t.Find("Locked");
////////        if (lockT != null) _lockOverlay = lockT.gameObject;

////////        var badgeT = t.Find("UpgradeBadge");
////////        if (badgeT != null)
////////        {
////////            _badgeRoot = badgeT.gameObject;
////////            // TMP may sit directly on the badge object or on a child Text object
////////            _badgeText = badgeT.GetComponent<TextMeshProUGUI>()
////////                      ?? badgeT.GetComponentInChildren<TextMeshProUGUI>(includeInactive: true);
////////        }

////////        _button = GetComponent<Button>();
////////        _button.onClick.AddListener(OnClick);

////////        SetSelected(false);
////////    }

////////    // ══════════════════════════════════════════════════════════════════════════
////////    // SETUP
////////    // ══════════════════════════════════════════════════════════════════════════

////////    /// <summary>
////////    /// Used for the 3 fixed Buy-mode cards.
////////    /// <paramref name="locked"/> = true until the player buys this type at least once.
////////    /// </summary>
////////    public void SetupBuyCard(CannonData data, bool locked)
////////    {
////////        _data = data;
////////        _inventoryId = -1;
////////        _isBuyMode = true;

////////        ApplySprite(data);
////////        SetCardName(data.cannonName);
////////        SetLocked(locked);
////////        ShowBadge(false, string.Empty);
////////        SetSelected(false);
////////    }

////////    /// <summary>
////////    /// Used for dynamically spawned Inventory-mode cards.
////////    /// <paramref name="displayName"/> is the copy-numbered label, e.g. "Iron Cannon (2/3)".
////////    /// </summary>
////////    public void SetupInventoryCard(CannonInventoryEntry entry, string displayName)
////////    {
////////        _data = entry.data;
////////        _inventoryId = entry.inventoryId;
////////        _isBuyMode = false;

////////        ApplySprite(entry.data);
////////        SetCardName(displayName);
////////        SetLocked(false);          // inventory cards are always owned — never locked
////////        RefreshBadge(entry);
////////        SetSelected(false);
////////    }

////////    // ══════════════════════════════════════════════════════════════════════════
////////    // RUNTIME REFRESH
////////    // ══════════════════════════════════════════════════════════════════════════

////////    /// <summary>Updates the upgrade badge. Called after an upgrade starts or completes.</summary>
////////    public void RefreshBadge(CannonInventoryEntry entry)
////////    {
////////        if (entry == null || _isBuyMode) { ShowBadge(false, string.Empty); return; }

////////        string text = entry.IsMaxLevel
////////            ? "MAX"
////////            : $"{entry.upgradeCount}/{CannonInventoryEntry.MAX_UPGRADES}";

////////        ShowBadge(true, text);
////////    }

////////    /// <summary>Shows or hides the lock overlay.</summary>
////////    public void SetLocked(bool locked)
////////    {
////////        if (_lockOverlay != null) _lockOverlay.SetActive(locked);
////////    }

////////    /// <summary>Shows or hides the selection highlight.</summary>
////////    public void SetSelected(bool selected)
////////    {
////////        if (_selectedHighlight != null) _selectedHighlight.SetActive(selected);
////////    }

////////    // ══════════════════════════════════════════════════════════════════════════
////////    // PRIVATE HELPERS
////////    // ══════════════════════════════════════════════════════════════════════════

////////    private void ApplySprite(CannonData data)
////////    {
////////        if (_cannonImage == null || data == null) return;

////////        Sprite s = data.previewSprite;
////////        if (s == null && data.idleSprites != null && data.idleSprites.Length > 0)
////////            s = data.idleSprites[0];

////////        if (s != null)
////////        {
////////            _cannonImage.sprite = s;
////////            _cannonImage.enabled = true;
////////        }
////////    }

////////    private void SetCardName(string text)
////////    {
////////        if (_nameText != null) _nameText.text = text;
////////    }

////////    private void ShowBadge(bool visible, string text)
////////    {
////////        if (_badgeRoot != null) _badgeRoot.SetActive(visible);
////////        if (_badgeText != null) _badgeText.text = text;
////////    }

////////    private void OnClick() => CannonPanelManager.Instance?.OnCardSelected(this);
////////}


//////using System.Collections;
//////using UnityEngine;
//////using UnityEngine.UI;
//////using TMPro;

///////// <summary>
///////// CANNON PANEL — CannonCard
/////////
///////// Attach this to the root of your CannonCard prefab.
///////// All child references are auto-wired by name in Awake() — no Inspector
///////// drag-and-drop required. Keep these exact child GameObject names:
/////////
/////////   CannonCard  (root — Button + this script)
/////////   ├── CannonImage      (Image component)
/////////   ├── CardName         (TextMeshProUGUI)
/////////   ├── Selected         (any GameObject — active when this card is selected)
/////////   ├── Locked           (any GameObject — active when type hasn't been bought yet)
/////////   └── UpgradeBadge     (GameObject with TextMeshProUGUI — shows "2/3" or "MAX")
/////////
///////// Card clicks are forwarded to CannonPanelManager.Instance.OnCardSelected(this).
///////// </summary>
//////[RequireComponent(typeof(Button))]
//////public class CannonCard : MonoBehaviour
//////{
//////    // ── Auto-wired at runtime ──────────────────────────────────────────────────
//////    public Image _cannonImage;
//////    public TextMeshProUGUI _nameText;
//////    public GameObject _selectedHighlight;
//////    public GameObject _lockOverlay;
//////    public GameObject _badgeRoot;
//////    public TextMeshProUGUI _badgeText;
//////    public Button _button;

//////    // ── Runtime data ───────────────────────────────────────────────────────────
//////    private CannonData _data;
//////    private int _inventoryId = -1;
//////    private bool _isBuyMode = true;

//////    // ── Badge pulse ────────────────────────────────────────────────────────────
//////    private Coroutine _pulseCoroutine;

//////    // Pulse settings — tweak these to taste
//////    private const float PulseMinScale = 0.85f;  // smallest scale during pulse
//////    private const float PulseMaxScale = 1.15f;  // largest scale during pulse
//////    private const float PulsePeriod = 0.7f;   // seconds for one full in-out cycle

//////    public CannonData Data => _data;
//////    public int InventoryId => _inventoryId;
//////    public bool IsBuyMode => _isBuyMode;

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // UNITY
//////    // ══════════════════════════════════════════════════════════════════════════

//////    private void Awake()
//////    {
//////        Transform t = transform;

//////        var imgT = t.Find("CannonImage");
//////        if (imgT != null) _cannonImage = imgT.GetComponent<Image>();

//////        var nameT = t.Find("CardName");
//////        if (nameT != null) _nameText = nameT.GetComponent<TextMeshProUGUI>();

//////        var selT = t.Find("Selected");
//////        if (selT != null) _selectedHighlight = selT.gameObject;

//////        var lockT = t.Find("Locked");
//////        if (lockT != null) _lockOverlay = lockT.gameObject;

//////        var badgeT = t.Find("UpgradeBadge");
//////        if (badgeT != null)
//////        {
//////            _badgeRoot = badgeT.gameObject;
//////            // TMP may sit directly on the badge object or on a child Text object
//////            _badgeText = badgeT.GetComponent<TextMeshProUGUI>()
//////                      ?? badgeT.GetComponentInChildren<TextMeshProUGUI>(includeInactive: true);
//////        }

//////        _button = GetComponent<Button>();
//////        _button.onClick.AddListener(OnClick);

//////        SetSelected(false);
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // SETUP
//////    // ══════════════════════════════════════════════════════════════════════════

//////    /// <summary>
//////    /// Used for the 3 fixed Buy-mode cards.
//////    /// <paramref name="locked"/> = true until the player buys this type at least once.
//////    /// </summary>
//////    public void SetupBuyCard(CannonData data, bool locked)
//////    {
//////        _data = data;
//////        _inventoryId = -1;
//////        _isBuyMode = true;

//////        ApplySprite(data);
//////        SetCardName(data.cannonName);
//////        SetLocked(locked);
//////        ShowBadge(false, string.Empty);
//////        SetSelected(false);
//////    }

//////    /// <summary>
//////    /// Used for dynamically spawned Inventory-mode cards.
//////    /// <paramref name="displayName"/> is the copy-numbered label, e.g. "Iron Cannon (2/3)".
//////    /// </summary>
//////    public void SetupInventoryCard(CannonInventoryEntry entry, string displayName)
//////    {
//////        _data = entry.data;
//////        _inventoryId = entry.inventoryId;
//////        _isBuyMode = false;

//////        ApplySprite(entry.data);
//////        SetCardName(displayName);
//////        SetLocked(false);          // inventory cards are always owned — never locked
//////        RefreshBadge(entry);
//////        SetSelected(false);
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // RUNTIME REFRESH
//////    // ══════════════════════════════════════════════════════════════════════════

//////    /// <summary>Updates the upgrade badge. Badge is ONLY visible while an upgrade is actively running.</summary>
//////    public void RefreshBadge(CannonInventoryEntry entry)
//////    {
//////        if (entry == null || _isBuyMode) { ShowBadge(false, string.Empty); return; }

//////        // Badge visible ONLY during an active upgrade, hidden at all other times
//////        if (entry.isUpgrading)
//////            ShowBadge(true, $"{entry.upgradeCount}/{CannonInventoryEntry.MAX_UPGRADES}");
//////        else
//////            ShowBadge(false, string.Empty);
//////    }

//////    /// <summary>Shows or hides the lock overlay.</summary>
//////    public void SetLocked(bool locked)
//////    {
//////        if (_lockOverlay != null) _lockOverlay.SetActive(locked);
//////    }

//////    /// <summary>Shows or hides the selection highlight.</summary>
//////    public void SetSelected(bool selected)
//////    {
//////        if (_selectedHighlight != null) _selectedHighlight.SetActive(selected);
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // PRIVATE HELPERS
//////    // ══════════════════════════════════════════════════════════════════════════

//////    private void ApplySprite(CannonData data)
//////    {
//////        if (_cannonImage == null) return;

//////        if (data == null)
//////        {
//////            _cannonImage.sprite = null;
//////            _cannonImage.enabled = false;
//////            return;
//////        }

//////        Sprite s = data.previewSprite;
//////        if (s == null && data.idleSprites != null && data.idleSprites.Length > 0)
//////            s = data.idleSprites[0];

//////        // Always overwrite sprite so a stale prefab sprite never bleeds through
//////        _cannonImage.sprite = s;
//////        _cannonImage.enabled = s != null;

//////#if UNITY_EDITOR
//////        if (s == null)
//////            Debug.LogWarning($"[CannonCard] '{data.cannonName}' has no previewSprite or idleSprites — " +
//////                             "assign a sprite in the CannonData ScriptableObject.", data);
//////        else
//////            Debug.Log($"[CannonCard] Sprite applied: {data.cannonName} → {s.name}", gameObject);
//////#endif
//////    }

//////    private void SetCardName(string text)
//////    {
//////        if (_nameText != null) _nameText.text = text;
//////    }

//////    /// <summary>
//////    /// Shows or hides the upgrade badge.
//////    /// When shown, starts a smooth scale-pulse coroutine on the badge root.
//////    /// When hidden, stops the pulse and resets scale to 1.
//////    /// </summary>
//////    private void ShowBadge(bool visible, string text)
//////    {
//////        if (_badgeRoot != null) _badgeRoot.SetActive(visible);
//////        if (_badgeText != null) _badgeText.text = text;

//////        if (visible)
//////            StartPulse();
//////        else
//////            StopPulse();
//////    }

//////    // ── Badge pulse helpers ────────────────────────────────────────────────────

//////    private void StartPulse()
//////    {
//////        if (_badgeRoot == null) return;
//////        StopPulse();   // ensure no duplicate coroutines
//////        _pulseCoroutine = StartCoroutine(PulseBadge());
//////    }

//////    private void StopPulse()
//////    {
//////        if (_pulseCoroutine != null)
//////        {
//////            StopCoroutine(_pulseCoroutine);
//////            _pulseCoroutine = null;
//////        }

//////        // Reset the badge scale so it doesn't stay mid-animation
//////        if (_badgeRoot != null)
//////            _badgeRoot.transform.localScale = Vector3.one;
//////    }

//////    /// <summary>
//////    /// Smoothly oscillates the badge scale between PulseMinScale and PulseMaxScale
//////    /// using a sine wave so the motion feels organic rather than linear.
//////    /// </summary>
//////    private IEnumerator PulseBadge()
//////    {
//////        Transform badgeT = _badgeRoot.transform;
//////        float t = 0f;

//////        while (true)
//////        {
//////            t += Time.deltaTime;
//////            // sin goes -1→1; remap to 0→1, then lerp between min and max scale
//////            float sin01 = (Mathf.Sin(t * (2f * Mathf.PI / PulsePeriod)) + 1f) * 0.5f;
//////            float scale = Mathf.Lerp(PulseMinScale, PulseMaxScale, sin01);
//////            badgeT.localScale = new Vector3(scale, scale, 1f);
//////            yield return null;
//////        }
//////    }

//////    private void OnClick() => CannonPanelManager.Instance?.OnCardSelected(this);
//////}

////using System.Collections;
////using UnityEngine;
////using UnityEngine.UI;
////using TMPro;

/////// <summary>
/////// CANNON PANEL — CannonCard
///////
/////// Attach this to the root of your LevelCardPrefab.
/////// All references are auto-wired by name in Awake() — no Inspector drag-and-drop needed.
///////
///////   LevelCardPrefab  (root — Button + CannonCard script)
///////   └── Cannon           (Image — cannon icon)
///////       ├── Level
///////       │   └── Text     (TextMeshProUGUI — card name label)
///////       ├── Selected     (any GameObject — active when this card is selected)
///////       ├── Lock         (any GameObject — active when type hasn't been bought yet)
///////       └── UpgradeBadge (Image — pulsing icon, shown ONLY while upgrading)
///////
/////// Card clicks are forwarded to CannonPanelManager.Instance.OnCardSelected(this).
/////// </summary>
////[RequireComponent(typeof(Button))]
////public class CannonCard : MonoBehaviour
////{
////    // ── Auto-wired at runtime ──────────────────────────────────────────────────
////    public Image _cannonImage;
////    public TextMeshProUGUI _nameText;
////    public GameObject _selectedHighlight;
////    public GameObject _lockOverlay;
////    public GameObject _badgeRoot;
////    public TextMeshProUGUI _badgeText;
////    public Button _button;

////    // ── Runtime data ───────────────────────────────────────────────────────────
////    private CannonData _data;
////    private int _inventoryId = -1;
////    private bool _isBuyMode = true;

////    // ── Badge pulse ────────────────────────────────────────────────────────────
////    private Coroutine _pulseCoroutine;

////    // Pulse settings — tweak these to taste
////    private const float PulseMinScale = 0.85f;  // smallest scale during pulse
////    private const float PulseMaxScale = 1.15f;  // largest scale during pulse
////    private const float PulsePeriod = 0.7f;   // seconds for one full in-out cycle

////    public CannonData Data => _data;
////    public int InventoryId => _inventoryId;
////    public bool IsBuyMode => _isBuyMode;

////    // ══════════════════════════════════════════════════════════════════════════
////    // UNITY
////    // ══════════════════════════════════════════════════════════════════════════

////    private void Awake()
////    {
////        Transform t = transform;

////        // "Cannon" is the direct child that holds the Image and all visual sub-children
////        Transform mid = t.Find("Cannon");

////        if (mid == null)
////            Debug.LogError("[CannonCard] Could not find child 'Cannon' on " +
////                           gameObject.name + ". Check the prefab hierarchy.", gameObject);

////        // Cannon icon Image is on the middle node itself, not the root
////        if (mid != null) _cannonImage = mid.GetComponent<Image>();

////        // All other children are one level under mid
////        // Note: Text is nested inside the "Level" child, not directly under "Cannon"
////        var nameT = mid != null ? mid.Find("Level/Text") : null;
////        var selT = mid != null ? mid.Find("Selected") : null;
////        var lockT = mid != null ? mid.Find("Lock") : null;
////        var badgeT = mid != null ? mid.Find("UpgradeBadge") : null;

////        if (nameT != null) _nameText = nameT.GetComponent<TextMeshProUGUI>();
////        if (selT != null) _selectedHighlight = selT.gameObject;
////        if (lockT != null) _lockOverlay = lockT.gameObject;
////        if (badgeT != null)
////        {
////            _badgeRoot = badgeT.gameObject;
////            _badgeText = null;   // badge is a plain Image — no TMP text
////        }

////        _button = GetComponent<Button>();
////        _button.onClick.AddListener(OnClick);

////        SetSelected(false);
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // SETUP
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>
////    /// Used for the 3 fixed Buy-mode cards.
////    /// <paramref name="locked"/> = true until the player buys this type at least once.
////    /// </summary>
////    public void SetupBuyCard(CannonData data, bool locked)
////    {
////        _data = data;
////        _inventoryId = -1;
////        _isBuyMode = true;

////        ApplySprite(data);
////        SetCardName(data.cannonName);
////        SetLocked(locked);
////        ShowBadge(false);
////        SetSelected(false);
////    }

////    /// <summary>
////    /// Used for dynamically spawned Inventory-mode cards.
////    /// <paramref name="displayName"/> is the copy-numbered label, e.g. "Iron Cannon (2/3)".
////    /// </summary>
////    public void SetupInventoryCard(CannonInventoryEntry entry, string displayName)
////    {
////        _data = entry.data;
////        _inventoryId = entry.inventoryId;
////        _isBuyMode = false;

////        ApplySprite(entry.data);
////        SetCardName(displayName);
////        SetLocked(false);          // inventory cards are always owned — never locked
////        RefreshBadge(entry);
////        SetSelected(false);
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // RUNTIME REFRESH
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>Updates the upgrade badge. Badge is ONLY visible while an upgrade is actively running.</summary>
////    public void RefreshBadge(CannonInventoryEntry entry)
////    {
////        if (entry == null || _isBuyMode) { ShowBadge(false); return; }

////        // Badge visible ONLY during an active upgrade, hidden at all other times
////        if (entry.isUpgrading)
////            ShowBadge(true);
////        else
////            ShowBadge(false);
////    }

////    /// <summary>Shows or hides the lock overlay.</summary>
////    public void SetLocked(bool locked)
////    {
////        if (_lockOverlay != null) _lockOverlay.SetActive(locked);
////    }

////    /// <summary>Shows or hides the selection highlight.</summary>
////    public void SetSelected(bool selected)
////    {
////        if (_selectedHighlight != null) _selectedHighlight.SetActive(selected);
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // PRIVATE HELPERS
////    // ══════════════════════════════════════════════════════════════════════════

////    private void ApplySprite(CannonData data)
////    {
////        if (_cannonImage == null) return;

////        if (data == null)
////        {
////            _cannonImage.sprite = null;
////            _cannonImage.enabled = false;
////            return;
////        }

////        Sprite s = data.previewSprite;
////        if (s == null && data.idleSprites != null && data.idleSprites.Length > 0)
////            s = data.idleSprites[0];

////        // Always overwrite sprite so a stale prefab sprite never bleeds through
////        _cannonImage.sprite = s;
////        _cannonImage.enabled = s != null;

////#if UNITY_EDITOR
////        if (s == null)
////            Debug.LogWarning($"[CannonCard] '{data.cannonName}' has no previewSprite or idleSprites — " +
////                             "assign a sprite in the CannonData ScriptableObject.", data);
////        else
////            Debug.Log($"[CannonCard] Sprite applied: {data.cannonName} → {s.name}", gameObject);
////#endif
////    }

////    private void SetCardName(string text)
////    {
////        if (_nameText != null) _nameText.text = text;
////    }

////    /// <summary>
////    /// Shows or hides the upgrade badge Image.
////    /// When shown, starts the scale-pulse coroutine.
////    /// When hidden, stops the pulse and resets scale to 1.
////    /// </summary>
////    private void ShowBadge(bool visible)
////    {
////        if (_badgeRoot != null) _badgeRoot.SetActive(visible);

////        if (visible)
////            StartPulse();
////        else
////            StopPulse();
////    }

////    // ── Badge pulse helpers ────────────────────────────────────────────────────

////    private void StartPulse()
////    {
////        if (_badgeRoot == null) return;
////        StopPulse();   // ensure no duplicate coroutines
////        _pulseCoroutine = StartCoroutine(PulseBadge());
////    }

////    private void StopPulse()
////    {
////        if (_pulseCoroutine != null)
////        {
////            StopCoroutine(_pulseCoroutine);
////            _pulseCoroutine = null;
////        }

////        // Reset the badge scale so it doesn't stay mid-animation
////        if (_badgeRoot != null)
////            _badgeRoot.transform.localScale = Vector3.one;
////    }

////    /// <summary>
////    /// Smoothly oscillates the badge scale between PulseMinScale and PulseMaxScale
////    /// using a sine wave so the motion feels organic rather than linear.
////    /// </summary>
////    private IEnumerator PulseBadge()
////    {
////        Transform badgeT = _badgeRoot.transform;
////        float t = 0f;

////        while (true)
////        {
////            t += Time.deltaTime;
////            // sin goes -1→1; remap to 0→1, then lerp between min and max scale
////            float sin01 = (Mathf.Sin(t * (2f * Mathf.PI / PulsePeriod)) + 1f) * 0.5f;
////            float scale = Mathf.Lerp(PulseMinScale, PulseMaxScale, sin01);
////            badgeT.localScale = new Vector3(scale, scale, 1f);
////            yield return null;
////        }
////    }

////    private void OnClick() => CannonPanelManager.Instance?.OnCardSelected(this);
////}

//using System.Collections;
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

///// <summary>
///// CANNON PANEL — CannonCard
/////
///// Attach this to the root of your LevelCardPrefab.
///// All references are auto-wired by name in Awake() — no Inspector drag-and-drop needed.
/////
/////   LevelCardPrefab  (root — Button + CannonCard script)
/////   └── Cannon           (Image — cannon icon)
/////       ├── Level
/////       │   └── Text     (TextMeshProUGUI — card name label)
/////       ├── Selected     (any GameObject — active when this card is selected)
/////       ├── Lock         (any GameObject — active when type hasn't been bought yet)
/////       └── UpgradeBadge (Image — pulsing icon, shown ONLY while upgrading)
/////
///// Card clicks are forwarded to CannonPanelManager.Instance.OnCardSelected(this).
///// </summary>
//[RequireComponent(typeof(Button))]
//public class CannonCard : MonoBehaviour
//{
//    // ── Auto-wired at runtime ──────────────────────────────────────────────────
//    public Image _cannonImage;
//    public TextMeshProUGUI _nameText;
//    public GameObject _selectedHighlight;
//    public GameObject _lockOverlay;
//    public GameObject _badgeRoot;
//    public TextMeshProUGUI _badgeText;
//    public Button _button;

//    // ── Runtime data ───────────────────────────────────────────────────────────
//    private CannonData _data;
//    private int _inventoryId = -1;
//    private bool _isBuyMode = true;

//    // ── Badge pulse ────────────────────────────────────────────────────────────
//    private Coroutine _pulseCoroutine;

//    // Pulse settings — tweak these to taste
//    private const float PulseMinScale = 0.85f;  // smallest scale during pulse
//    private const float PulseMaxScale = 1.15f;  // largest scale during pulse
//    private const float PulsePeriod = 0.7f;   // seconds for one full in-out cycle

//    public CannonData Data => _data;
//    public int InventoryId => _inventoryId;
//    public bool IsBuyMode => _isBuyMode;

//    // ══════════════════════════════════════════════════════════════════════════
//    // UNITY
//    // ══════════════════════════════════════════════════════════════════════════

//    private void Awake()
//    {
//        Transform t = transform;

//        // "Cannon" is a direct child of the root — find it safely (works even when inactive)
//        Transform mid = FindDirectChild(t, "Cannon");

//        if (mid == null)
//            Debug.LogError("[CannonCard] Could not find child 'Cannon' on " +
//                           gameObject.name + ". Check the prefab hierarchy.", gameObject);

//        // Cannon icon — Image component sits on the "Cannon" node itself
//        if (mid != null) _cannonImage = mid.GetComponent<Image>();

//        // Text is nested: Cannon → Level → Text
//        Transform levelT = mid != null ? FindDirectChild(mid, "Level") : null;
//        Transform nameT = levelT != null ? FindDirectChild(levelT, "Text") : null;
//        if (nameT != null) _nameText = nameT.GetComponent<TextMeshProUGUI>();

//        // Selected, Lock, UpgradeBadge are direct children of Cannon.
//        // Using FindDirectChild so inactive GameObjects are always found.
//        Transform selT = mid != null ? FindDirectChild(mid, "Selected") : null;
//        Transform lockT = mid != null ? FindDirectChild(mid, "Lock") : null;
//        Transform badgeT = mid != null ? FindDirectChild(mid, "UpgradeBadge") : null;

//        if (selT != null) _selectedHighlight = selT.gameObject;
//        if (lockT != null) _lockOverlay = lockT.gameObject;
//        if (badgeT != null) { _badgeRoot = badgeT.gameObject; _badgeText = null; }

//        _button = GetComponent<Button>();
//        _button.onClick.AddListener(OnClick);

//        SetSelected(false);
//    }

//    /// <summary>
//    /// Finds a direct child of <paramref name="parent"/> by name.
//    /// Unlike Transform.Find(), this iterates GetChild() so it reliably
//    /// returns inactive GameObjects regardless of Unity version.
//    /// </summary>
//    private static Transform FindDirectChild(Transform parent, string childName)
//    {
//        for (int i = 0; i < parent.childCount; i++)
//        {
//            Transform child = parent.GetChild(i);
//            if (child.name == childName) return child;
//        }
//        return null;
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // SETUP
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Used for the 3 fixed Buy-mode cards.
//    /// <paramref name="locked"/> = true until the player buys this type at least once.
//    /// </summary>
//    public void SetupBuyCard(CannonData data, bool locked)
//    {
//        _data = data;
//        _inventoryId = -1;
//        _isBuyMode = true;

//        ApplySprite(data);
//        SetCardName(data.cannonName);
//        SetLocked(locked);
//        ShowBadge(false);
//        SetSelected(false);
//    }

//    /// <summary>
//    /// Used for dynamically spawned Inventory-mode cards.
//    /// <paramref name="displayName"/> is the copy-numbered label, e.g. "Iron Cannon (2/3)".
//    /// </summary>
//    public void SetupInventoryCard(CannonInventoryEntry entry, string displayName)
//    {
//        _data = entry.data;
//        _inventoryId = entry.inventoryId;
//        _isBuyMode = false;

//        ApplySprite(entry.data);
//        SetCardName(displayName);
//        SetLocked(false);          // inventory cards are always owned — never locked
//        RefreshBadge(entry);
//        SetSelected(false);
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // RUNTIME REFRESH
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>Updates the upgrade badge. Badge is ONLY visible while an upgrade is actively running.</summary>
//    public void RefreshBadge(CannonInventoryEntry entry)
//    {
//        if (entry == null || _isBuyMode) { ShowBadge(false); return; }

//        // Badge visible ONLY during an active upgrade, hidden at all other times
//        if (entry.isUpgrading)
//            ShowBadge(true);
//        else
//            ShowBadge(false);
//    }

//    /// <summary>Shows or hides the lock overlay.</summary>
//    public void SetLocked(bool locked)
//    {
//        if (_lockOverlay != null) _lockOverlay.SetActive(locked);
//    }

//    /// <summary>Shows or hides the selection highlight.</summary>
//    public void SetSelected(bool selected)
//    {
//        if (_selectedHighlight != null) _selectedHighlight.SetActive(selected);
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // PRIVATE HELPERS
//    // ══════════════════════════════════════════════════════════════════════════

//    private void ApplySprite(CannonData data)
//    {
//        if (_cannonImage == null) return;

//        if (data == null)
//        {
//            _cannonImage.sprite = null;
//            _cannonImage.enabled = false;
//            return;
//        }

//        Sprite s = data.previewSprite;
//        if (s == null && data.idleSprites != null && data.idleSprites.Length > 0)
//            s = data.idleSprites[0];

//        // Always overwrite sprite so a stale prefab sprite never bleeds through
//        _cannonImage.sprite = s;
//        _cannonImage.enabled = s != null;

//#if UNITY_EDITOR
//        if (s == null)
//            Debug.LogWarning($"[CannonCard] '{data.cannonName}' has no previewSprite or idleSprites — " +
//                             "assign a sprite in the CannonData ScriptableObject.", data);
//        else
//            Debug.Log($"[CannonCard] Sprite applied: {data.cannonName} → {s.name}", gameObject);
//#endif
//    }

//    private void SetCardName(string text)
//    {
//        if (_nameText != null) _nameText.text = text;
//    }

//    /// <summary>
//    /// Shows or hides the upgrade badge Image.
//    /// When shown, starts the scale-pulse coroutine.
//    /// When hidden, stops the pulse and resets scale to 1.
//    /// </summary>
//    private void ShowBadge(bool visible)
//    {
//        if (_badgeRoot != null) _badgeRoot.SetActive(visible);

//        if (visible)
//            StartPulse();
//        else
//            StopPulse();
//    }

//    // ── Badge pulse helpers ────────────────────────────────────────────────────

//    private void StartPulse()
//    {
//        if (_badgeRoot == null) return;
//        StopPulse();   // ensure no duplicate coroutines
//        _pulseCoroutine = StartCoroutine(PulseBadge());
//    }

//    private void StopPulse()
//    {
//        if (_pulseCoroutine != null)
//        {
//            StopCoroutine(_pulseCoroutine);
//            _pulseCoroutine = null;
//        }

//        // Reset the badge scale so it doesn't stay mid-animation
//        if (_badgeRoot != null)
//            _badgeRoot.transform.localScale = Vector3.one;
//    }

//    /// <summary>
//    /// Smoothly oscillates the badge scale between PulseMinScale and PulseMaxScale
//    /// using a sine wave so the motion feels organic rather than linear.
//    /// </summary>
//    private IEnumerator PulseBadge()
//    {
//        Transform badgeT = _badgeRoot.transform;
//        float t = 0f;

//        while (true)
//        {
//            t += Time.deltaTime;
//            // sin goes -1→1; remap to 0→1, then lerp between min and max scale
//            float sin01 = (Mathf.Sin(t * (2f * Mathf.PI / PulsePeriod)) + 1f) * 0.5f;
//            float scale = Mathf.Lerp(PulseMinScale, PulseMaxScale, sin01);
//            badgeT.localScale = new Vector3(scale, scale, 1f);
//            yield return null;
//        }
//    }

//    private void OnClick() => CannonPanelManager.Instance?.OnCardSelected(this);
//}

//////using UnityEngine;
//////using UnityEngine.UI;
//////using TMPro;

///////// <summary>
///////// CANNON PANEL — CannonCard
/////////
///////// Attach this to the root of your CannonCard prefab.
///////// All child references are auto-wired by name in Awake() — no Inspector
///////// drag-and-drop required. Keep these exact child GameObject names:
/////////
/////////   CannonCard  (root — Button + this script)
/////////   ├── CannonImage      (Image component)
/////////   ├── CardName         (TextMeshProUGUI)
/////////   ├── Selected         (any GameObject — active when this card is selected)
/////////   ├── Locked           (any GameObject — active when type hasn't been bought yet)
/////////   └── UpgradeBadge     (GameObject with TextMeshProUGUI — shows "2/3" or "MAX")
/////////
///////// Card clicks are forwarded to CannonPanelManager.Instance.OnCardSelected(this).
///////// </summary>
//////[RequireComponent(typeof(Button))]
//////public class CannonCard : MonoBehaviour
//////{
//////    // ── Auto-wired at runtime ──────────────────────────────────────────────────
//////    public Image _cannonImage;
//////    public TextMeshProUGUI _nameText;
//////    public GameObject _selectedHighlight;
//////    public GameObject _lockOverlay;
//////    public GameObject _badgeRoot;
//////    public TextMeshProUGUI _badgeText;
//////    public Button _button;

//////    // ── Runtime data ───────────────────────────────────────────────────────────
//////    private CannonData _data;
//////    private int _inventoryId = -1;
//////    private bool _isBuyMode = true;

//////    public CannonData Data => _data;
//////    public int InventoryId => _inventoryId;
//////    public bool IsBuyMode => _isBuyMode;

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // UNITY
//////    // ══════════════════════════════════════════════════════════════════════════

//////    private void Awake()
//////    {
//////        Transform t = transform;

//////        var imgT = t.Find("CannonImage");
//////        if (imgT != null) _cannonImage = imgT.GetComponent<Image>();

//////        var nameT = t.Find("CardName");
//////        if (nameT != null) _nameText = nameT.GetComponent<TextMeshProUGUI>();

//////        var selT = t.Find("Selected");
//////        if (selT != null) _selectedHighlight = selT.gameObject;

//////        var lockT = t.Find("Locked");
//////        if (lockT != null) _lockOverlay = lockT.gameObject;

//////        var badgeT = t.Find("UpgradeBadge");
//////        if (badgeT != null)
//////        {
//////            _badgeRoot = badgeT.gameObject;
//////            // TMP may sit directly on the badge object or on a child Text object
//////            _badgeText = badgeT.GetComponent<TextMeshProUGUI>()
//////                      ?? badgeT.GetComponentInChildren<TextMeshProUGUI>(includeInactive: true);
//////        }

//////        _button = GetComponent<Button>();
//////        _button.onClick.AddListener(OnClick);

//////        SetSelected(false);
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // SETUP
//////    // ══════════════════════════════════════════════════════════════════════════

//////    /// <summary>
//////    /// Used for the 3 fixed Buy-mode cards.
//////    /// <paramref name="locked"/> = true until the player buys this type at least once.
//////    /// </summary>
//////    public void SetupBuyCard(CannonData data, bool locked)
//////    {
//////        _data = data;
//////        _inventoryId = -1;
//////        _isBuyMode = true;

//////        ApplySprite(data);
//////        SetCardName(data.cannonName);
//////        SetLocked(locked);
//////        ShowBadge(false, string.Empty);
//////        SetSelected(false);
//////    }

//////    /// <summary>
//////    /// Used for dynamically spawned Inventory-mode cards.
//////    /// <paramref name="displayName"/> is the copy-numbered label, e.g. "Iron Cannon (2/3)".
//////    /// </summary>
//////    public void SetupInventoryCard(CannonInventoryEntry entry, string displayName)
//////    {
//////        _data = entry.data;
//////        _inventoryId = entry.inventoryId;
//////        _isBuyMode = false;

//////        ApplySprite(entry.data);
//////        SetCardName(displayName);
//////        SetLocked(false);          // inventory cards are always owned — never locked
//////        RefreshBadge(entry);
//////        SetSelected(false);
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // RUNTIME REFRESH
//////    // ══════════════════════════════════════════════════════════════════════════

//////    /// <summary>Updates the upgrade badge. Called after an upgrade starts or completes.</summary>
//////    public void RefreshBadge(CannonInventoryEntry entry)
//////    {
//////        if (entry == null || _isBuyMode) { ShowBadge(false, string.Empty); return; }

//////        string text = entry.IsMaxLevel
//////            ? "MAX"
//////            : $"{entry.upgradeCount}/{CannonInventoryEntry.MAX_UPGRADES}";

//////        ShowBadge(true, text);
//////    }

//////    /// <summary>Shows or hides the lock overlay.</summary>
//////    public void SetLocked(bool locked)
//////    {
//////        if (_lockOverlay != null) _lockOverlay.SetActive(locked);
//////    }

//////    /// <summary>Shows or hides the selection highlight.</summary>
//////    public void SetSelected(bool selected)
//////    {
//////        if (_selectedHighlight != null) _selectedHighlight.SetActive(selected);
//////    }

//////    // ══════════════════════════════════════════════════════════════════════════
//////    // PRIVATE HELPERS
//////    // ══════════════════════════════════════════════════════════════════════════

//////    private void ApplySprite(CannonData data)
//////    {
//////        if (_cannonImage == null || data == null) return;

//////        Sprite s = data.previewSprite;
//////        if (s == null && data.idleSprites != null && data.idleSprites.Length > 0)
//////            s = data.idleSprites[0];

//////        if (s != null)
//////        {
//////            _cannonImage.sprite = s;
//////            _cannonImage.enabled = true;
//////        }
//////    }

//////    private void SetCardName(string text)
//////    {
//////        if (_nameText != null) _nameText.text = text;
//////    }

//////    private void ShowBadge(bool visible, string text)
//////    {
//////        if (_badgeRoot != null) _badgeRoot.SetActive(visible);
//////        if (_badgeText != null) _badgeText.text = text;
//////    }

//////    private void OnClick() => CannonPanelManager.Instance?.OnCardSelected(this);
//////}


////using System.Collections;
////using UnityEngine;
////using UnityEngine.UI;
////using TMPro;

/////// <summary>
/////// CANNON PANEL — CannonCard
///////
/////// Attach this to the root of your CannonCard prefab.
/////// All child references are auto-wired by name in Awake() — no Inspector
/////// drag-and-drop required. Keep these exact child GameObject names:
///////
///////   CannonCard  (root — Button + this script)
///////   ├── CannonImage      (Image component)
///////   ├── CardName         (TextMeshProUGUI)
///////   ├── Selected         (any GameObject — active when this card is selected)
///////   ├── Locked           (any GameObject — active when type hasn't been bought yet)
///////   └── UpgradeBadge     (GameObject with TextMeshProUGUI — shows "2/3" or "MAX")
///////
/////// Card clicks are forwarded to CannonPanelManager.Instance.OnCardSelected(this).
/////// </summary>
////[RequireComponent(typeof(Button))]
////public class CannonCard : MonoBehaviour
////{
////    // ── Auto-wired at runtime ──────────────────────────────────────────────────
////    public Image _cannonImage;
////    public TextMeshProUGUI _nameText;
////    public GameObject _selectedHighlight;
////    public GameObject _lockOverlay;
////    public GameObject _badgeRoot;
////    public TextMeshProUGUI _badgeText;
////    public Button _button;

////    // ── Runtime data ───────────────────────────────────────────────────────────
////    private CannonData _data;
////    private int _inventoryId = -1;
////    private bool _isBuyMode = true;

////    // ── Badge pulse ────────────────────────────────────────────────────────────
////    private Coroutine _pulseCoroutine;

////    // Pulse settings — tweak these to taste
////    private const float PulseMinScale = 0.85f;  // smallest scale during pulse
////    private const float PulseMaxScale = 1.15f;  // largest scale during pulse
////    private const float PulsePeriod = 0.7f;   // seconds for one full in-out cycle

////    public CannonData Data => _data;
////    public int InventoryId => _inventoryId;
////    public bool IsBuyMode => _isBuyMode;

////    // ══════════════════════════════════════════════════════════════════════════
////    // UNITY
////    // ══════════════════════════════════════════════════════════════════════════

////    private void Awake()
////    {
////        Transform t = transform;

////        var imgT = t.Find("CannonImage");
////        if (imgT != null) _cannonImage = imgT.GetComponent<Image>();

////        var nameT = t.Find("CardName");
////        if (nameT != null) _nameText = nameT.GetComponent<TextMeshProUGUI>();

////        var selT = t.Find("Selected");
////        if (selT != null) _selectedHighlight = selT.gameObject;

////        var lockT = t.Find("Locked");
////        if (lockT != null) _lockOverlay = lockT.gameObject;

////        var badgeT = t.Find("UpgradeBadge");
////        if (badgeT != null)
////        {
////            _badgeRoot = badgeT.gameObject;
////            // TMP may sit directly on the badge object or on a child Text object
////            _badgeText = badgeT.GetComponent<TextMeshProUGUI>()
////                      ?? badgeT.GetComponentInChildren<TextMeshProUGUI>(includeInactive: true);
////        }

////        _button = GetComponent<Button>();
////        _button.onClick.AddListener(OnClick);

////        SetSelected(false);
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // SETUP
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>
////    /// Used for the 3 fixed Buy-mode cards.
////    /// <paramref name="locked"/> = true until the player buys this type at least once.
////    /// </summary>
////    public void SetupBuyCard(CannonData data, bool locked)
////    {
////        _data = data;
////        _inventoryId = -1;
////        _isBuyMode = true;

////        ApplySprite(data);
////        SetCardName(data.cannonName);
////        SetLocked(locked);
////        ShowBadge(false, string.Empty);
////        SetSelected(false);
////    }

////    /// <summary>
////    /// Used for dynamically spawned Inventory-mode cards.
////    /// <paramref name="displayName"/> is the copy-numbered label, e.g. "Iron Cannon (2/3)".
////    /// </summary>
////    public void SetupInventoryCard(CannonInventoryEntry entry, string displayName)
////    {
////        _data = entry.data;
////        _inventoryId = entry.inventoryId;
////        _isBuyMode = false;

////        ApplySprite(entry.data);
////        SetCardName(displayName);
////        SetLocked(false);          // inventory cards are always owned — never locked
////        RefreshBadge(entry);
////        SetSelected(false);
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // RUNTIME REFRESH
////    // ══════════════════════════════════════════════════════════════════════════

////    /// <summary>Updates the upgrade badge. Badge is ONLY visible while an upgrade is actively running.</summary>
////    public void RefreshBadge(CannonInventoryEntry entry)
////    {
////        if (entry == null || _isBuyMode) { ShowBadge(false, string.Empty); return; }

////        // Badge visible ONLY during an active upgrade, hidden at all other times
////        if (entry.isUpgrading)
////            ShowBadge(true, $"{entry.upgradeCount}/{CannonInventoryEntry.MAX_UPGRADES}");
////        else
////            ShowBadge(false, string.Empty);
////    }

////    /// <summary>Shows or hides the lock overlay.</summary>
////    public void SetLocked(bool locked)
////    {
////        if (_lockOverlay != null) _lockOverlay.SetActive(locked);
////    }

////    /// <summary>Shows or hides the selection highlight.</summary>
////    public void SetSelected(bool selected)
////    {
////        if (_selectedHighlight != null) _selectedHighlight.SetActive(selected);
////    }

////    // ══════════════════════════════════════════════════════════════════════════
////    // PRIVATE HELPERS
////    // ══════════════════════════════════════════════════════════════════════════

////    private void ApplySprite(CannonData data)
////    {
////        if (_cannonImage == null) return;

////        if (data == null)
////        {
////            _cannonImage.sprite = null;
////            _cannonImage.enabled = false;
////            return;
////        }

////        Sprite s = data.previewSprite;
////        if (s == null && data.idleSprites != null && data.idleSprites.Length > 0)
////            s = data.idleSprites[0];

////        // Always overwrite sprite so a stale prefab sprite never bleeds through
////        _cannonImage.sprite = s;
////        _cannonImage.enabled = s != null;

////#if UNITY_EDITOR
////        if (s == null)
////            Debug.LogWarning($"[CannonCard] '{data.cannonName}' has no previewSprite or idleSprites — " +
////                             "assign a sprite in the CannonData ScriptableObject.", data);
////        else
////            Debug.Log($"[CannonCard] Sprite applied: {data.cannonName} → {s.name}", gameObject);
////#endif
////    }

////    private void SetCardName(string text)
////    {
////        if (_nameText != null) _nameText.text = text;
////    }

////    /// <summary>
////    /// Shows or hides the upgrade badge.
////    /// When shown, starts a smooth scale-pulse coroutine on the badge root.
////    /// When hidden, stops the pulse and resets scale to 1.
////    /// </summary>
////    private void ShowBadge(bool visible, string text)
////    {
////        if (_badgeRoot != null) _badgeRoot.SetActive(visible);
////        if (_badgeText != null) _badgeText.text = text;

////        if (visible)
////            StartPulse();
////        else
////            StopPulse();
////    }

////    // ── Badge pulse helpers ────────────────────────────────────────────────────

////    private void StartPulse()
////    {
////        if (_badgeRoot == null) return;
////        StopPulse();   // ensure no duplicate coroutines
////        _pulseCoroutine = StartCoroutine(PulseBadge());
////    }

////    private void StopPulse()
////    {
////        if (_pulseCoroutine != null)
////        {
////            StopCoroutine(_pulseCoroutine);
////            _pulseCoroutine = null;
////        }

////        // Reset the badge scale so it doesn't stay mid-animation
////        if (_badgeRoot != null)
////            _badgeRoot.transform.localScale = Vector3.one;
////    }

////    /// <summary>
////    /// Smoothly oscillates the badge scale between PulseMinScale and PulseMaxScale
////    /// using a sine wave so the motion feels organic rather than linear.
////    /// </summary>
////    private IEnumerator PulseBadge()
////    {
////        Transform badgeT = _badgeRoot.transform;
////        float t = 0f;

////        while (true)
////        {
////            t += Time.deltaTime;
////            // sin goes -1→1; remap to 0→1, then lerp between min and max scale
////            float sin01 = (Mathf.Sin(t * (2f * Mathf.PI / PulsePeriod)) + 1f) * 0.5f;
////            float scale = Mathf.Lerp(PulseMinScale, PulseMaxScale, sin01);
////            badgeT.localScale = new Vector3(scale, scale, 1f);
////            yield return null;
////        }
////    }

////    private void OnClick() => CannonPanelManager.Instance?.OnCardSelected(this);
////}

//using System.Collections;
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

///// <summary>
///// CANNON PANEL — CannonCard
/////
///// Attach this to the root of your LevelCardPrefab.
///// All references are auto-wired by name in Awake() — no Inspector drag-and-drop needed.
/////
/////   LevelCardPrefab  (root — Button + CannonCard script)
/////   └── Cannon           (Image — cannon icon)
/////       ├── Level
/////       │   └── Text     (TextMeshProUGUI — card name label)
/////       ├── Selected     (any GameObject — active when this card is selected)
/////       ├── Lock         (any GameObject — active when type hasn't been bought yet)
/////       └── UpgradeBadge (Image — pulsing icon, shown ONLY while upgrading)
/////
///// Card clicks are forwarded to CannonPanelManager.Instance.OnCardSelected(this).
///// </summary>
//[RequireComponent(typeof(Button))]
//public class CannonCard : MonoBehaviour
//{
//    // ── Auto-wired at runtime ──────────────────────────────────────────────────
//    public Image _cannonImage;
//    public TextMeshProUGUI _nameText;
//    public GameObject _selectedHighlight;
//    public GameObject _lockOverlay;
//    public GameObject _badgeRoot;
//    public TextMeshProUGUI _badgeText;
//    public Button _button;

//    // ── Runtime data ───────────────────────────────────────────────────────────
//    private CannonData _data;
//    private int _inventoryId = -1;
//    private bool _isBuyMode = true;

//    // ── Badge pulse ────────────────────────────────────────────────────────────
//    private Coroutine _pulseCoroutine;

//    // Pulse settings — tweak these to taste
//    private const float PulseMinScale = 0.85f;  // smallest scale during pulse
//    private const float PulseMaxScale = 1.15f;  // largest scale during pulse
//    private const float PulsePeriod = 0.7f;   // seconds for one full in-out cycle

//    public CannonData Data => _data;
//    public int InventoryId => _inventoryId;
//    public bool IsBuyMode => _isBuyMode;

//    // ══════════════════════════════════════════════════════════════════════════
//    // UNITY
//    // ══════════════════════════════════════════════════════════════════════════

//    private void Awake()
//    {
//        Transform t = transform;

//        // "Cannon" is the direct child that holds the Image and all visual sub-children
//        Transform mid = t.Find("Cannon");

//        if (mid == null)
//            Debug.LogError("[CannonCard] Could not find child 'Cannon' on " +
//                           gameObject.name + ". Check the prefab hierarchy.", gameObject);

//        // Cannon icon Image is on the middle node itself, not the root
//        if (mid != null) _cannonImage = mid.GetComponent<Image>();

//        // All other children are one level under mid
//        // Note: Text is nested inside the "Level" child, not directly under "Cannon"
//        var nameT = mid != null ? mid.Find("Level/Text") : null;
//        var selT = mid != null ? mid.Find("Selected") : null;
//        var lockT = mid != null ? mid.Find("Lock") : null;
//        var badgeT = mid != null ? mid.Find("UpgradeBadge") : null;

//        if (nameT != null) _nameText = nameT.GetComponent<TextMeshProUGUI>();
//        if (selT != null) _selectedHighlight = selT.gameObject;
//        if (lockT != null) _lockOverlay = lockT.gameObject;
//        if (badgeT != null)
//        {
//            _badgeRoot = badgeT.gameObject;
//            _badgeText = null;   // badge is a plain Image — no TMP text
//        }

//        _button = GetComponent<Button>();
//        _button.onClick.AddListener(OnClick);

//        SetSelected(false);
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // SETUP
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>
//    /// Used for the 3 fixed Buy-mode cards.
//    /// <paramref name="locked"/> = true until the player buys this type at least once.
//    /// </summary>
//    public void SetupBuyCard(CannonData data, bool locked)
//    {
//        _data = data;
//        _inventoryId = -1;
//        _isBuyMode = true;

//        ApplySprite(data);
//        SetCardName(data.cannonName);
//        SetLocked(locked);
//        ShowBadge(false);
//        SetSelected(false);
//    }

//    /// <summary>
//    /// Used for dynamically spawned Inventory-mode cards.
//    /// <paramref name="displayName"/> is the copy-numbered label, e.g. "Iron Cannon (2/3)".
//    /// </summary>
//    public void SetupInventoryCard(CannonInventoryEntry entry, string displayName)
//    {
//        _data = entry.data;
//        _inventoryId = entry.inventoryId;
//        _isBuyMode = false;

//        ApplySprite(entry.data);
//        SetCardName(displayName);
//        SetLocked(false);          // inventory cards are always owned — never locked
//        RefreshBadge(entry);
//        SetSelected(false);
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // RUNTIME REFRESH
//    // ══════════════════════════════════════════════════════════════════════════

//    /// <summary>Updates the upgrade badge. Badge is ONLY visible while an upgrade is actively running.</summary>
//    public void RefreshBadge(CannonInventoryEntry entry)
//    {
//        if (entry == null || _isBuyMode) { ShowBadge(false); return; }

//        // Badge visible ONLY during an active upgrade, hidden at all other times
//        if (entry.isUpgrading)
//            ShowBadge(true);
//        else
//            ShowBadge(false);
//    }

//    /// <summary>Shows or hides the lock overlay.</summary>
//    public void SetLocked(bool locked)
//    {
//        if (_lockOverlay != null) _lockOverlay.SetActive(locked);
//    }

//    /// <summary>Shows or hides the selection highlight.</summary>
//    public void SetSelected(bool selected)
//    {
//        if (_selectedHighlight != null) _selectedHighlight.SetActive(selected);
//    }

//    // ══════════════════════════════════════════════════════════════════════════
//    // PRIVATE HELPERS
//    // ══════════════════════════════════════════════════════════════════════════

//    private void ApplySprite(CannonData data)
//    {
//        if (_cannonImage == null) return;

//        if (data == null)
//        {
//            _cannonImage.sprite = null;
//            _cannonImage.enabled = false;
//            return;
//        }

//        Sprite s = data.previewSprite;
//        if (s == null && data.idleSprites != null && data.idleSprites.Length > 0)
//            s = data.idleSprites[0];

//        // Always overwrite sprite so a stale prefab sprite never bleeds through
//        _cannonImage.sprite = s;
//        _cannonImage.enabled = s != null;

//#if UNITY_EDITOR
//        if (s == null)
//            Debug.LogWarning($"[CannonCard] '{data.cannonName}' has no previewSprite or idleSprites — " +
//                             "assign a sprite in the CannonData ScriptableObject.", data);
//        else
//            Debug.Log($"[CannonCard] Sprite applied: {data.cannonName} → {s.name}", gameObject);
//#endif
//    }

//    private void SetCardName(string text)
//    {
//        if (_nameText != null) _nameText.text = text;
//    }

//    /// <summary>
//    /// Shows or hides the upgrade badge Image.
//    /// When shown, starts the scale-pulse coroutine.
//    /// When hidden, stops the pulse and resets scale to 1.
//    /// </summary>
//    private void ShowBadge(bool visible)
//    {
//        if (_badgeRoot != null) _badgeRoot.SetActive(visible);

//        if (visible)
//            StartPulse();
//        else
//            StopPulse();
//    }

//    // ── Badge pulse helpers ────────────────────────────────────────────────────

//    private void StartPulse()
//    {
//        if (_badgeRoot == null) return;
//        StopPulse();   // ensure no duplicate coroutines
//        _pulseCoroutine = StartCoroutine(PulseBadge());
//    }

//    private void StopPulse()
//    {
//        if (_pulseCoroutine != null)
//        {
//            StopCoroutine(_pulseCoroutine);
//            _pulseCoroutine = null;
//        }

//        // Reset the badge scale so it doesn't stay mid-animation
//        if (_badgeRoot != null)
//            _badgeRoot.transform.localScale = Vector3.one;
//    }

//    /// <summary>
//    /// Smoothly oscillates the badge scale between PulseMinScale and PulseMaxScale
//    /// using a sine wave so the motion feels organic rather than linear.
//    /// </summary>
//    private IEnumerator PulseBadge()
//    {
//        Transform badgeT = _badgeRoot.transform;
//        float t = 0f;

//        while (true)
//        {
//            t += Time.deltaTime;
//            // sin goes -1→1; remap to 0→1, then lerp between min and max scale
//            float sin01 = (Mathf.Sin(t * (2f * Mathf.PI / PulsePeriod)) + 1f) * 0.5f;
//            float scale = Mathf.Lerp(PulseMinScale, PulseMaxScale, sin01);
//            badgeT.localScale = new Vector3(scale, scale, 1f);
//            yield return null;
//        }
//    }

//    private void OnClick() => CannonPanelManager.Instance?.OnCardSelected(this);
//}

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// CANNON PANEL — CannonCard
///
/// Attach this to the root of your LevelCardPrefab.
/// All references are auto-wired by name in Awake() — no Inspector drag-and-drop needed.
///
///   LevelCardPrefab  (root — Button + CannonCard script)
///   └── Cannon           (Image — cannon icon)
///       ├── Level
///       │   └── Text     (TextMeshProUGUI — card name label)
///       ├── Selected     (any GameObject — active when this card is selected)
///       ├── Lock         (any GameObject — active when type hasn't been bought yet)
///       └── UpgradeBadge (Image — pulsing icon, shown ONLY while upgrading)
///
/// Card clicks are forwarded to CannonPanelManager.Instance.OnCardSelected(this).
/// </summary>
[RequireComponent(typeof(Button))]
public class CannonCard : MonoBehaviour
{
    // ── Auto-wired at runtime ──────────────────────────────────────────────────
    public Image _cannonImage;
    public TextMeshProUGUI _nameText;
    public GameObject _selectedHighlight;
    public GameObject _lockOverlay;
    public GameObject _badgeRoot;
    public TextMeshProUGUI _badgeText;
    public Button _button;

    // ── Runtime data ───────────────────────────────────────────────────────────
    private CannonData _data;
    private int _inventoryId = -1;
    private bool _isBuyMode = true;

    // ── Badge pulse ────────────────────────────────────────────────────────────
    private Coroutine _pulseCoroutine;

    // Pulse settings — tweak these to taste
    private const float PulseMinScale = 0.85f;  // smallest scale during pulse
    private const float PulseMaxScale = 1.15f;  // largest scale during pulse
    private const float PulsePeriod = 0.7f;   // seconds for one full in-out cycle

    public CannonData Data => _data;
    public int InventoryId => _inventoryId;
    public bool IsBuyMode => _isBuyMode;

    // ══════════════════════════════════════════════════════════════════════════
    // UNITY
    // ══════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        Transform t = transform;

        // "Cannon" is a direct child of the root — find it safely (works even when inactive)
        Transform mid = FindDirectChild(t, "Cannon");

        if (mid == null)
            Debug.LogError("[CannonCard] Could not find child 'Cannon' on " +
                           gameObject.name + ". Check the prefab hierarchy.", gameObject);

        // Cannon icon — Image component sits on the "Cannon" node itself
        if (mid != null) _cannonImage = mid.GetComponent<Image>();

        // Text is nested: Cannon → Level → Text
        Transform levelT = mid != null ? FindDirectChild(mid, "Level") : null;
        Transform nameT = levelT != null ? FindDirectChild(levelT, "Text") : null;
        if (nameT != null) _nameText = nameT.GetComponent<TextMeshProUGUI>();

        // Selected, Lock, UpgradeBadge are direct children of Cannon.
        // Using FindDirectChild so inactive GameObjects are always found.
        Transform selT = mid != null ? FindDirectChild(mid, "Selected") : null;
        Transform lockT = mid != null ? FindDirectChild(mid, "Lock") : null;
        Transform badgeT = mid != null ? FindDirectChild(mid, "UpgradeBadge") : null;

        if (selT != null) _selectedHighlight = selT.gameObject;
        if (lockT != null) _lockOverlay = lockT.gameObject;
        if (badgeT != null) { _badgeRoot = badgeT.gameObject; _badgeText = null; }

        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnClick);

        SetSelected(false);
    }

    /// <summary>
    /// Finds a direct child of <paramref name="parent"/> by name.
    /// Unlike Transform.Find(), this iterates GetChild() so it reliably
    /// returns inactive GameObjects regardless of Unity version.
    /// </summary>
    private static Transform FindDirectChild(Transform parent, string childName)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName) return child;
        }
        return null;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // SETUP
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Used for the 3 fixed Buy-mode cards.
    /// <paramref name="locked"/> = true until the player buys this type at least once.
    /// </summary>
    public void SetupBuyCard(CannonData data, bool locked)
    {
        _data = data;
        _inventoryId = -1;
        _isBuyMode = true;

        ApplySprite(data);
        SetCardName(data.cannonName);
        SetLocked(locked);
        ShowBadge(false);
        SetSelected(false);
    }

    /// <summary>
    /// Used for dynamically spawned Inventory-mode cards.
    /// <paramref name="displayName"/> is the copy-numbered label, e.g. "Iron Cannon (2/3)".
    /// </summary>
    public void SetupInventoryCard(CannonInventoryEntry entry, string displayName)
    {
        _data = entry.data;
        _inventoryId = entry.inventoryId;
        _isBuyMode = false;

        ApplySprite(entry.data);
        SetCardName(displayName);
        SetLocked(false);          // inventory cards are always owned — never locked
        RefreshBadge(entry);
        SetSelected(false);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // RUNTIME REFRESH
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Updates the upgrade badge. Badge is ONLY visible while an upgrade is actively running.</summary>
    public void RefreshBadge(CannonInventoryEntry entry)
    {
        if (entry == null || _isBuyMode) { ShowBadge(false); return; }

        // Badge visible ONLY during an active upgrade, hidden at all other times
        if (entry.isUpgrading)
            ShowBadge(true);
        else
            ShowBadge(false);
    }

    /// <summary>Shows or hides the lock overlay.</summary>
    public void SetLocked(bool locked)
    {
        if (_lockOverlay != null) _lockOverlay.SetActive(locked);
    }

    /// <summary>Shows or hides the selection highlight.</summary>
    public void SetSelected(bool selected)
    {
        if (_selectedHighlight != null) _selectedHighlight.SetActive(selected);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ══════════════════════════════════════════════════════════════════════════

    private void ApplySprite(CannonData data)
    {
        if (_cannonImage == null) return;

        if (data == null)
        {
            _cannonImage.sprite = null;
            _cannonImage.enabled = false;
            return;
        }

        Sprite s = data.previewSprite;
        if (s == null && data.idleSprites != null && data.idleSprites.Length > 0)
            s = data.idleSprites[0];

        // Always overwrite sprite so a stale prefab sprite never bleeds through
        _cannonImage.sprite = s;
        _cannonImage.enabled = s != null;

#if UNITY_EDITOR
        if (s == null)
            Debug.LogWarning($"[CannonCard] '{data.cannonName}' has no previewSprite or idleSprites — " +
                             "assign a sprite in the CannonData ScriptableObject.", data);
        else
            Debug.Log($"[CannonCard] Sprite applied: {data.cannonName} → {s.name}", gameObject);
#endif
    }

    private void SetCardName(string text)
    {
        if (_nameText != null) _nameText.text = text;
    }

    /// <summary>
    /// Shows or hides the upgrade badge Image.
    /// When shown, starts the scale-pulse coroutine.
    /// When hidden, stops the pulse and resets scale to 1.
    /// </summary>
    private void ShowBadge(bool visible)
    {
        if (_badgeRoot != null) _badgeRoot.SetActive(visible);

        if (visible)
            StartPulse();
        else
            StopPulse();
    }

    // ── Badge pulse helpers ────────────────────────────────────────────────────

    private void StartPulse()
    {
        if (_badgeRoot == null) return;
        StopPulse();   // ensure no duplicate coroutines
        _pulseCoroutine = StartCoroutine(PulseBadge());
    }

    private void StopPulse()
    {
        if (_pulseCoroutine != null)
        {
            StopCoroutine(_pulseCoroutine);
            _pulseCoroutine = null;
        }

        // Reset the badge scale so it doesn't stay mid-animation
        if (_badgeRoot != null)
            _badgeRoot.transform.localScale = Vector3.one;
    }

    /// <summary>
    /// Smoothly oscillates the badge scale between PulseMinScale and PulseMaxScale
    /// using a sine wave so the motion feels organic rather than linear.
    /// </summary>
    private IEnumerator PulseBadge()
    {
        Transform badgeT = _badgeRoot.transform;
        float t = 0f;

        while (true)
        {
            t += Time.deltaTime;
            // sin goes -1→1; remap to 0→1, then lerp between min and max scale
            float sin01 = (Mathf.Sin(t * (2f * Mathf.PI / PulsePeriod)) + 1f) * 0.5f;
            float scale = Mathf.Lerp(PulseMinScale, PulseMaxScale, sin01);
            badgeT.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
    }

    private void OnClick() => CannonPanelManager.Instance?.OnCardSelected(this);
}