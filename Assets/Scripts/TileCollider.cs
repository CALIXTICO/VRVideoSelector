using UnityEngine;
[RequireComponent(typeof(BoxCollider))]
public class TileColliderSync : MonoBehaviour
{
    private RectTransform rt;
    private BoxCollider col;

    void Awake()
    {
        rt  = GetComponent<RectTransform>();
        col = GetComponent<BoxCollider>();
        Sync();
    }

    void OnRectTransformDimensionsChange() => Sync();

    void Sync()
    {
        if (!rt || !col) return;
        var size = rt.rect.size;
        // Convertimos píxeles del Canvas a unidades mundo (escala de RectTransform)
        var sx = size.x * rt.lossyScale.x;
        var sy = size.y * rt.lossyScale.y;
        // Un grosor mínimo para el collider:
        col.size = new Vector3(sx, sy, 0.01f);
        col.center = Vector3.zero;
        col.isTrigger = true;
        // Asegúrate de que el layer del tile es el mismo que uses en el LayerMask del raycaster.
    }
}

