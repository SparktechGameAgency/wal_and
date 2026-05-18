using System.Collections;
using UnityEngine;

public class ProjectileArc : MonoBehaviour
{
    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void Launch(Vector3 start, Vector3 end, float arcHeight, float duration)
    {
        StartCoroutine(MoveInArc(start, end, arcHeight, duration));
    }

    IEnumerator MoveInArc(Vector3 start, Vector3 end, float arcHeight, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Horizontal lerp
            Vector3 pos = Vector3.Lerp(start, end, t);

            // Vertical arc parabola
            pos.y += arcHeight * 4f * t * (1f - t);

            rectTransform.position = pos;

            yield return null;
        }

        rectTransform.position = end;

        GetComponent<ProjectileBlast>()?.Explode();
    }
}