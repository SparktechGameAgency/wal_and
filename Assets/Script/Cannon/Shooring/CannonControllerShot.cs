using UnityEngine;
using UnityEngine.UI;

public class CannonControllerShot : MonoBehaviour
{
    [Header("References")]
    public SpriteAnimator cannonAnimator;
    public RectTransform projectileSpawner;
    public GameObject projectilePrefab;
    public Button fireButton;
    public RectTransform target;
    public Canvas canvas;

    [Header("Arc Settings")]
    public float arcHeight = 150f;
    public float flightDuration = 2f;

    private bool isFiring = false;

    void Start()
    {
        fireButton.onClick.AddListener(OnFireClicked);

        cannonAnimator.onSpawnFrame = SpawnProjectile;
        cannonAnimator.onComplete = () => isFiring = false;
    }

    void OnFireClicked()
    {
        if (isFiring) return;
        isFiring = true;
        cannonAnimator.Play();
    }

    void SpawnProjectile()
    {
        GameObject proj = Instantiate(
            projectilePrefab,
            canvas.transform
        );

        RectTransform projRect = proj.GetComponent<RectTransform>();
        projRect.position = projectileSpawner.position;

        ProjectileArc arc = proj.GetComponent<ProjectileArc>();
        if (arc != null)
            arc.Launch(projectileSpawner.position, target.position, arcHeight, flightDuration);
    }
}