using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CastleBlockHUD : MonoBehaviour
{
    [Header("Bar References")]
    public Slider healthBar;
    public Slider shieldBar;
    public Slider durabilityBar;

    [Header("Bar Fill Colors")]
    public Image healthFill;
    public Image shieldFill;
    public Image durabilityFill;

    public Color healthColor = Color.red;
    public Color shieldColor = Color.cyan;
    public Color durabilityColor = Color.yellow;

    [Header("Label (optional)")]
    public TextMeshProUGUI blockNameLabel;

    private CastleBlock _block;

    // Make the HUD always face the camera
    private Camera _cam;

    private void Awake()
    {
        _cam = Camera.main;

        if (healthFill != null) healthFill.color = healthColor;
        if (shieldFill != null) shieldFill.color = shieldColor;
        if (durabilityFill != null) durabilityFill.color = durabilityColor;
    }

    public void Bind(CastleBlock block)
    {
        _block = block;
        _block.OnStatsChanged += Refresh;

        if (blockNameLabel != null)
            blockNameLabel.text = block.blockName;

        Refresh(block);
    }

    private void LateUpdate()
    {
        // Billboard: always face camera
        if (_cam != null)
            transform.LookAt(transform.position + _cam.transform.rotation * Vector3.forward,
                             _cam.transform.rotation * Vector3.up);
    }

    void Refresh(CastleBlock block)
    {
        if (healthBar != null) healthBar.value = block.HealthNormalized;
        if (shieldBar != null) shieldBar.value = block.ShieldNormalized;
        if (durabilityBar != null) durabilityBar.value = block.DurabilityNormalized;
    }

    private void OnDestroy()
    {
        if (_block != null) _block.OnStatsChanged -= Refresh;
    }
}