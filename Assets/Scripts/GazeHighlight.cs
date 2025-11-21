using UnityEngine;
using UnityEngine.UI;            // Image, RawImage heredan de Graphic
using UnityEngine.EventSystems;

public class GazeHighlight : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    [Header("Target a resaltar (Graphic: Image o RawImage)")]
    [SerializeField] private Graphic target;        // <- sirve para Image o RawImage

    [Header("Efecto visual")]
    [SerializeField] private float scaleUp = 1.06f;
    [SerializeField] private float lerpSpeed = 12f;
    [SerializeField] private Color tint = new Color(1f, 1f, 1f, 0.20f);
    [SerializeField] private bool addOutline = true;

    private Outline outline;
    private Vector3 baseScale;
    private Color baseColor;
    private bool hovering;

    private void Awake()
    {
        if (target == null) target = GetComponent<Graphic>(); // usa el Graphic del mismo objeto si no asignas
        if (target != null) baseColor = target.color;

        baseScale = transform.localScale;

        if (addOutline)
        {
            outline = GetComponent<Outline>();
            if (outline == null) outline = gameObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(6f, 6f);
            outline.useGraphicAlpha = true;
            outline.enabled = false;
        }
    }

    private void Update()
    {
        var desired = hovering ? baseScale * scaleUp : baseScale;
        transform.localScale = Vector3.Lerp(transform.localScale, desired, Time.unscaledDeltaTime * lerpSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;
        if (target != null) target.color = baseColor + new Color(tint.r, tint.g, tint.b, tint.a);
        if (outline != null) outline.enabled = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
        if (target != null) target.color = baseColor;
        if (outline != null) outline.enabled = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        transform.localScale = baseScale * 0.98f;
    }

    public void OnPointerUp(PointerEventData eventData) { }
}
