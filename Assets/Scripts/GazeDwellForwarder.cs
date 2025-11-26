using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class GazeDwellForwarder : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private VideoTile _tile;

    void Awake()
    {
        _tile = GetComponent<VideoTile>();
        if (!_tile)
        {
            Debug.LogWarning("[GazeDwellForwarder] No se encontró VideoTile en este objeto.");
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _tile?.BeginGaze();   // dispara el preHold + fill ya implementados en VideoTile
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _tile?.EndGaze();     // cancela/resetea la rueda
    }
}
