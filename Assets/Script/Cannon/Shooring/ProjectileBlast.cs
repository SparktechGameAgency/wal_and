using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ProjectileBlast : MonoBehaviour
{
    [Header("Blast Animation")]
    public SpriteAnimator blastAnimator;

    public void Explode()
    {
        StartCoroutine(BlastSequence());
    }

    IEnumerator BlastSequence()
    {
        // Hide projectile image
        Image projImage = GetComponent<Image>();
        if (projImage != null)
            projImage.enabled = false;

        if (blastAnimator != null)
        {
            blastAnimator.gameObject.SetActive(true);
            blastAnimator.Play();

            yield return new WaitForSeconds(blastAnimator.GetDuration());
        }
        else
        {
            yield return null;
        }

        Destroy(gameObject);
    }
}