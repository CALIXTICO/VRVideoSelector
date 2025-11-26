using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class VideoTile : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Refs")]
    [SerializeField] private RawImage thumbnail;         // RawImage para la miniatura
    [SerializeField] private Image progress;             // Image Filled (Radial 360)
    [SerializeField] private TextMeshProUGUI title;      // Título

    [Header("Gaze Timing (seconds)")]
    [SerializeField, Tooltip("Tiempo mirando antes de mostrar la rueda")]
    private float preHoldTime = 2f;                      // anillo aparece a los 2 s
    [SerializeField, Tooltip("Tiempo que tarda en llenarse la rueda")]
    private float fillTime = 3f;                         // se llena entre 2–5 s

    [Header("Placeholder (opcional)")]
    [SerializeField, Tooltip("Textura antes de que cargue la miniatura")]
    private Texture2D placeholderTexture;

    private VideoItem _item;
    private Action<VideoItem> _onSelected;

    // Corutinas existentes
    private Coroutine _gazeRoutine;

    // Conteo de tiempo observado
    private Coroutine _tickRoutine;
    private float _totalObservedSeconds = 0f;   // sumatoria acumulada por este tile
    private float _currentDwellSeconds = 0f;    // tiempo del dwell actual

    public void Setup(VideoItem item, Action<VideoItem> onSelected)
    {
        _item = item;
        _onSelected = onSelected;

        // Título
        if (title) title.text = item?.Title ?? string.Empty;

        // Progreso (inicia oculto) — asegurar config y que no bloquee raycasts
        if (progress)
        {
            progress.type = Image.Type.Filled;
            progress.fillMethod = Image.FillMethod.Radial360;
            // Usa el origen que prefieras en el prefab; lo dejamos como está:
            // progress.fillOrigin = (int)Image.Origin360.Top;
            progress.fillAmount = 0f;
            progress.enabled = false;
            progress.raycastTarget = false; // no bloquear GraphicRaycaster/UI gaze
            var c = progress.color;         // garantizar alpha 1
            progress.color = new Color(c.r, c.g, c.b, 1f);
        }

        // Asegurar layout seguro del tile y capas de dibujo
        ApplySafeLayout();

        // Placeholder visible de inmediato
        if (thumbnail)
        {
            if (placeholderTexture) thumbnail.texture = placeholderTexture;
            thumbnail.enabled = true;
        }

        // Cargar miniatura con fallbacks (maxres/hq/sd/mq/default)
        if (!string.IsNullOrEmpty(item?.YoutubeId))
        {
            StartCoroutine(LoadThumbnailWithFallbacks(item.YoutubeId));
        }
        else
        {
            Debug.LogWarning("[VideoTile] YoutubeId vacío.");
        }
    }

    // ========= Gaze API =========
    public void BeginGaze()
    {
        // arrancar la rutina de selección (prehold + fill)
        if (_gazeRoutine != null) StopCoroutine(_gazeRoutine);
        _gazeRoutine = StartCoroutine(GazeSelectRoutine());

        // arrancar conteo por segundo
        if (_tickRoutine != null) StopCoroutine(_tickRoutine);
        _currentDwellSeconds = 0f;
        _tickRoutine = StartCoroutine(DwellTickRoutine());
    }

    public void EndGaze()
    {
        // parar selección
        if (_gazeRoutine != null) StopCoroutine(_gazeRoutine);
        _gazeRoutine = null;

        // parar conteo por segundo
        if (_tickRoutine != null) StopCoroutine(_tickRoutine);
        _tickRoutine = null;

        // reset visual de la rueda
        if (progress) { progress.fillAmount = 0f; progress.enabled = false; }
    }

    public void OnPointerEnter(PointerEventData eventData) => BeginGaze();
    public void OnPointerExit(PointerEventData eventData)  => EndGaze();

    private void OnDisable()
    {
        if (_gazeRoutine != null)
        {
            StopCoroutine(_gazeRoutine);
            _gazeRoutine = null;
        }
        if (_tickRoutine != null)
        {
            StopCoroutine(_tickRoutine);
            _tickRoutine = null;
        }
        if (progress) { progress.fillAmount = 0f; progress.enabled = false; }
    }

    // ========= Internos =========

    private void ApplySafeLayout()
    {
        // El root del prefab debe tener RectTransform y (opcional) un Image como fondo
        var rootRT = transform as RectTransform;
        if (rootRT != null)
        {
            rootRT.localScale = Vector3.one;                 // nada de 0.01
            rootRT.localRotation = Quaternion.identity;
        }

        // THUMBNAIL ocupa toda la celda (stretch con offsets 0)
        if (thumbnail)
        {
            var rt = thumbnail.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            thumbnail.color = Color.white;       // alpha 255
            thumbnail.maskable = true;

            // Orden de dibujo: fondo (si existe) < THUMBNAIL < Progress/Title
            thumbnail.transform.SetAsFirstSibling();
        }

        // PROGRESS también ocupa toda la celda y va arriba
        if (progress)
        {
            var prt = progress.rectTransform;
            prt.anchorMin = Vector2.zero;
            prt.anchorMax = Vector2.one;
            prt.offsetMin = Vector2.zero;
            prt.offsetMax = Vector2.zero;
            progress.maskable = true;
            progress.transform.SetAsLastSibling();
        }

        if (title) title.transform.SetAsLastSibling();

        // Por si algún padre tuviera CanvasGroup con alpha 0
        var cg = GetComponent<CanvasGroup>();
        if (cg) cg.alpha = 1f;
    }

    private IEnumerator LoadThumbnailWithFallbacks(string youtubeId)
    {
        string Url(string q) => $"https://img.youtube.com/vi/{youtubeId}/{q}.jpg";
        var urls = new string[]
        {
            Url("maxresdefault"),
            Url("hqdefault"),
            Url("sddefault"),
            Url("mqdefault"),
            Url("default")
        };

        Texture2D tex = null;
        foreach (var u in urls)
        {
            using (var req = UnityWebRequestTexture.GetTexture(u))
            {
                yield return req.SendWebRequest();
#if UNITY_2020_1_OR_NEWER
                if (req.result == UnityWebRequest.Result.Success)
#else
                if (!req.isNetworkError && !req.isHttpError)
#endif
                {
                    tex = DownloadHandlerTexture.GetContent(req);
                    Debug.Log($"[VideoTile] Thumbnail OK: {u} ({tex.width}x{tex.height})");
                    break;
                }
                else
                {
                    Debug.LogWarning($"[VideoTile] Thumbnail intento fallido: {u} -> {req.error}");
                }
            }
        }

        if (tex != null && thumbnail)
        {
            thumbnail.texture = tex;
            thumbnail.enabled = true;

            // Reasegura orden (por si el prefab se reordenó)
            thumbnail.transform.SetAsFirstSibling();
            if (progress) progress.transform.SetAsLastSibling();
            if (title)    title.transform.SetAsLastSibling();
        }
    }

    private IEnumerator GazeSelectRoutine()
    {
        float t = 0f;
        if (progress) { progress.fillAmount = 0f; progress.enabled = false; }

        // pre-hold (no mostrar anillo aún)
        while (t < preHoldTime)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        // rueda visible y al frente
        if (progress)
        {
            progress.enabled = true;
            progress.transform.SetAsLastSibling();
            progress.fillAmount = 0f;
        }

        // fill (de 0 a 1 en 'fillTime')
        float p = 0f;
        while (p < 1f)
        {
            p += Time.unscaledDeltaTime / Mathf.Max(0.001f, fillTime);
            if (progress) progress.fillAmount = Mathf.Clamp01(p);
            yield return null;
        }

        // selección completada
        _onSelected?.Invoke(_item);
    }

    // Imprime cada 1s la sumatoria acumulada y el dwell actual
    private IEnumerator DwellTickRoutine()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(1f);
            _currentDwellSeconds += 1f;
            _totalObservedSeconds += 1f;

            string nameForLog = !string.IsNullOrEmpty(_item?.Title) ? _item.Title : gameObject.name;
            Debug.Log($"[Dwell] {nameForLog} -> total={_totalObservedSeconds:F0}s (actual={_currentDwellSeconds:F0}s)");
        }
    }
}
