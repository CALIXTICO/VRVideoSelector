using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GazePhysicsRaycaster : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private EyeGazeProvider gazeProvider;  // arrastra tu EyeGazeProvider
    [SerializeField] private Camera eventCamera;            // normalmente la Main Camera del XR Origin

    [Header("Ray settings")]
    [SerializeField] private float maxDistance = 15f;
    [SerializeField] private LayerMask tileMask = ~0;       // filtra a la capa de los tiles (p.ej. GazeUI)
    [SerializeField] private bool log = true;

    private VideoTile _currentTile;
    private Collider  _currentCollider;
    private PointerEventData _ped;                          // para simular enter/exit UI
    private readonly List<RaycastResult> _tmp = new();      // no lo usamos para UI real, sólo para formalidad

    void Awake()
    {
        if (!eventCamera) eventCamera = Camera.main;
        if (!gazeProvider) gazeProvider = FindFirstObjectByType<EyeGazeProvider>();

        if (!EventSystem.current)
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            if (log) Debug.LogWarning("[GazePhysics] No había EventSystem; se creó uno.");
        }

        _ped = new PointerEventData(EventSystem.current);
    }

    void Update()
    {
        if (!gazeProvider || !eventCamera)
        {
            ClearFocus();
            return;
        }

        if (!gazeProvider.TryGetEyeGazeRay(out var ray))
        {
            ClearFocus();
            return;
        }

        if (Physics.Raycast(ray, out var hit, maxDistance, tileMask, QueryTriggerInteraction.Collide))
        {
            var col  = hit.collider;
            var tile = col ? col.GetComponentInParent<VideoTile>() : null;

            // Posición de puntero para los enter/exit simulados
            var screenPos = eventCamera.WorldToScreenPoint(hit.point);
            _ped.Reset();
            _ped.position = screenPos;

            if (tile != _currentTile)
            {
                // exit del anterior
                if (_currentTile)
                {
                    ExecuteEvents.ExecuteHierarchy(_currentTile.gameObject, _ped, ExecuteEvents.pointerExitHandler);
                    _currentTile.EndGaze();
                    if (log) Debug.Log($"[GazePhysics] Exit: {_currentTile.name}");
                }

                _currentTile    = tile;
                _currentCollider = col;

                if (_currentTile)
                {
                    ExecuteEvents.ExecuteHierarchy(_currentTile.gameObject, _ped, ExecuteEvents.pointerEnterHandler);
                    _currentTile.BeginGaze();
                    if (log) Debug.Log($"[GazePhysics] Enter: {_currentTile.name}");
                }
            }
            else
            {
                // steady hit (opcional log suave)
                // if (log && _currentTile) Debug.Log($"[GazePhysics] (steady) {_currentTile.name}");
            }
        }
        else
        {
            ClearFocus();
        }
    }

    void ClearFocus()
    {
        if (_currentTile != null)
        {
            ExecuteEvents.ExecuteHierarchy(_currentTile.gameObject, _ped, ExecuteEvents.pointerExitHandler);
            _currentTile.EndGaze();
            if (log) Debug.Log($"[GazePhysics] Exit: {_currentTile.name}");
        }
        _currentTile = null;
        _currentCollider = null;
    }
}
