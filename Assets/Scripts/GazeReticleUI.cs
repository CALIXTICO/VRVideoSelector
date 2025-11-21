using UnityEngine;
using UnityEngine.UI;  // Image
using UnityEngine.EventSystems;

public class GazeReticleUI : MonoBehaviour
{
    [SerializeField] private RectTransform reticle;   // círculo UI dentro del Canvas
    [SerializeField] private float size = 12f;

    /// <summary>
    /// Crea el retículo si no existe, como hijo del RectTransform del Canvas.
    /// </summary>
    public void Ensure(RectTransform canvasRT)
    {
        if (reticle != null) return;

        var go = new GameObject("GazeReticle", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(canvasRT, worldPositionStays: false);

        reticle = go.GetComponent<RectTransform>();
        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        img.color = new Color(1f, 1f, 1f, 0.85f); // blanco semitransparente

        reticle.sizeDelta = new Vector2(size, size);
        reticle.anchorMin = reticle.anchorMax = new Vector2(0.5f, 0.5f);
        reticle.pivot = new Vector2(0.5f, 0.5f);
        reticle.anchoredPosition = Vector2.zero;
    }

    /// <summary>
    /// Mueve el retículo a la posición de pantalla mapeada al Canvas.
    /// </summary>
    public void SetScreenPos(Vector2 screenPos, Camera eventCam)
    {
        if (!reticle || !eventCam) return;

        var canvas = reticle.GetComponentInParent<Canvas>();
        if (!canvas) return;

        // Convertimos posición de pantalla → punto local en el RectTransform del Canvas
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                screenPos,
                eventCam,
                out Vector2 local))
        {
            reticle.anchoredPosition = local;
        }
    }

    public void Show(bool v)
    {
        if (reticle) reticle.gameObject.SetActive(v);
    }
}
