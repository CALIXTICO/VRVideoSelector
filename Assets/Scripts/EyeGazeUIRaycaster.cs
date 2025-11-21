using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EyeGazeUIRaycaster : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private EyeGazeProvider gazeProvider;   // arrastra tu EyeGazeProvider
    [SerializeField] private Canvas worldCanvas;             // tu Canvas (World Space) que contiene las tiles
    [SerializeField] private GraphicRaycaster uiRaycaster;   // GraphicRaycaster del Canvas
    [SerializeField] private Camera eventCamera;             // normalmente Main Camera

    [Header("Ray settings")]
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private LayerMask physicsMask = ~0;     // fallback con colliders
    [SerializeField] private bool logHits = false;

    private PointerEventData _ped;
    private readonly List<RaycastResult> _uiResults = new();
    private Plane _canvasPlane;

    // Estado estricto de selección única
    private VideoTile _currentTile;          // VideoTile actualmente “mirada”
    private GameObject _currentTileGO;       // root GO del VideoTile actual (para enter/exit)

    void Awake()
    {
        if (!eventCamera) eventCamera = Camera.main;
        if (!gazeProvider) gazeProvider = FindFirstObjectByType<EyeGazeProvider>();
        if (!worldCanvas) worldCanvas = FindFirstObjectByType<Canvas>();
        if (!uiRaycaster && worldCanvas) uiRaycaster = worldCanvas.GetComponent<GraphicRaycaster>();

        if (!EventSystem.current)
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Debug.LogWarning("[GazeUI] No había EventSystem. Se creó uno.");
        }

        _ped = new PointerEventData(EventSystem.current);

        if (worldCanvas)
            _canvasPlane = new Plane(worldCanvas.transform.forward, worldCanvas.transform.position);
    }

    void Update()
    {
        if (!TryGetRay(out var ray))
        {
            HandleSelection(null, null);   // limpia selección
            LogHit("(none)");
            return;
        }

        // 1) UI Raycast contra Canvas (GraphicRaycaster requiere posición en pantalla):
        if (TryUIRaycast(ray, out var uiHitGO, out var uiHitScreenPos))
        {
            // Resuelve el VideoTile “root” desde el objeto gráfico golpeado
            var newTile = GetVideoTileFromGO(uiHitGO, out var tileRootGO);
            _ped.Reset();
            _ped.position = uiHitScreenPos;   // posición para enter/exit coherente
            HandleSelection(newTile, tileRootGO);
            LogHit(newTile ? newTile.name : "(none)");
            return;
        }

        // 2) Fallback: Physics raycast (requiere BoxCollider en tiles o hijos):
        if (Physics.Raycast(ray, out var hit, maxDistance, physicsMask, QueryTriggerInteraction.Collide))
        {
            var go = hit.collider ? hit.collider.gameObject : null;
            var newTile = GetVideoTileFromGO(go, out var tileRootGO);

            // Convertimos punto de impacto a screenPos para los eventos UI (forzando Vector2)
            Vector2 screenPos;
            if (eventCamera)
            {
                Vector3 sp3 = eventCamera.WorldToScreenPoint(hit.point);
                screenPos = new Vector2(sp3.x, sp3.y);
            }
            else
            {
                screenPos = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            }

            _ped.Reset();
            _ped.position = screenPos;

            HandleSelection(newTile, tileRootGO);
            LogHit(newTile ? newTile.name : "(none)");
            return;
        }

        // 3) Nada golpeado → limpiar selección
        HandleSelection(null, null);
        LogHit("(none)");
    }

    // ---------------- Helpers principales ----------------

    bool TryGetRay(out Ray ray)
    {
        ray = default;
        if (!gazeProvider) return false;
        return gazeProvider.TryGetEyeGazeRay(out ray);
    }

    bool TryUIRaycast(Ray ray, out GameObject overGO, out Vector2 screenPos)
    {
        overGO = null;
        screenPos = default;

        if (!worldCanvas || !uiRaycaster || !eventCamera)
            return false;

        // Intersección rayo ↔ plano del canvas:
        if (!_canvasPlane.Raycast(ray, out var t))
            return false;

        if (t < 0f || t > maxDistance)
            return false;

        var worldPoint = ray.origin + ray.direction * t;
        Vector3 sp3 = eventCamera.WorldToScreenPoint(worldPoint);
        screenPos = new Vector2(sp3.x, sp3.y);

        _ped.Reset();
        _ped.position = screenPos;

        _uiResults.Clear();
        uiRaycaster.Raycast(_ped, _uiResults);

        if (_uiResults.Count > 0)
        {
            overGO = _uiResults[0].gameObject; // puede ser un hijo (Image/Text/etc)
            return true;
        }
        return false;
    }

    /// <summary>
    /// Devuelve el VideoTile (si existe) y su GO raíz (el que tiene el componente).
    /// </summary>
    VideoTile GetVideoTileFromGO(GameObject go, out GameObject tileRootGO)
    {
        tileRootGO = null;
        if (!go) return null;

        var tile = go.GetComponentInParent<VideoTile>();
        if (tile)
        {
            tileRootGO = tile.gameObject;
            return tile;
        }
        return null;
    }

    /// <summary>
    /// Aplica la política de selección única:
    /// - Si la tile nueva es distinta a la actual:
    ///     * hace Exit en la anterior (UI + lógica)
    ///     * hace Enter en la nueva (UI + lógica)
    /// - Si es la misma, no hace nada (evita spam).
    /// - Si es null, limpia selección (Exit si había actual).
    /// </summary>
    void HandleSelection(VideoTile newTile, GameObject newTileGO)
    {
        if (newTile == _currentTile) return; // no repetir enters en la misma tile

        // Exit del anterior (UI + lógica)
        if (_currentTile)
        {
            // UI: pointerExit en el ROOT del VideoTile
            if (_currentTileGO)
                ExecuteEvents.Execute(_currentTileGO, _ped, ExecuteEvents.pointerExitHandler);

            // Lógica de la tile (detiene rueda, counters, etc.)
            _currentTile.EndGaze();
        }

        _currentTile = newTile;
        _currentTileGO = newTileGO;

        // Enter del nuevo (UI + lógica)
        if (_currentTile)
        {
            // UI: pointerEnter en el ROOT del VideoTile
            if (_currentTileGO)
                ExecuteEvents.Execute(_currentTileGO, _ped, ExecuteEvents.pointerEnterHandler);

            // Lógica de la tile (inicia rueda, counters, etc.)
            _currentTile.BeginGaze();
        }
    }

    void LogHit(string name)
    {
        if (logHits)
            Debug.Log($"[GazeUI] Hit: {name}");
    }

    // Por si cambias la posición/orientación del Canvas en runtime:
    public void RebuildCanvasPlane()
    {
        if (worldCanvas)
            _canvasPlane = new Plane(worldCanvas.transform.forward, worldCanvas.transform.position);
    }
}
