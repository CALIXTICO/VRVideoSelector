using UnityEngine;

public class VideoGridController : MonoBehaviour
{
    [Header("Data")]
    public VideoList videoList;

    [Header("UI")]
    public Transform gridParent;          // Panel con GridLayoutGroup
    public GameObject videoTilePrefab;    // Prefab del tile

    private void Start()
    {
        BuildGrid();
    }

    public void BuildGrid()
    {
        if (!videoList) { Debug.LogError("[VideoGridController] Falta VideoList"); return; }
        if (!gridParent) { Debug.LogError("[VideoGridController] Falta GridParent"); return; }
        if (!videoTilePrefab) { Debug.LogError("[VideoGridController] Falta VideoTilePrefab"); return; }

        for (int i = gridParent.childCount - 1; i >= 0; i--)
            Destroy(gridParent.GetChild(i).gameObject);

        foreach (var item in videoList.Items)
        {
            var go = Instantiate(videoTilePrefab, gridParent);
            var rt = go.transform as RectTransform;
            if (rt != null)
            {
                rt.localScale = Vector3.one;                 // evita escala rara
                rt.localRotation = Quaternion.identity;      // evita rotaciones raras
            }

            var tile = go.GetComponent<VideoTile>();
            if (!tile)
            {
                Debug.LogError("[VideoGridController] El prefab no tiene componente VideoTile");
                continue;
            }

            tile.Setup(item, OnVideoSelected);
        }
    }

    private void OnVideoSelected(VideoItem item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.YoutubeId))
        {
            Debug.LogWarning("[Select] VideoItem nulo o YoutubeId vacío.");
            return;
        }

        // URL estándar: en Quest abrirá YouTube/YouTube VR y el propio video 360 se presentará en modo 360
        string webUrl = $"https://www.youtube.com/watch?v={item.YoutubeId}&vr=1";

        // (Opcional) En Android puedes intentar el esquema del app. Si prefieres, cambia a 'vnd.youtube:'.
        // Ojo: Application.OpenURL no reporta fallo, así que usamos una sola opción para evitar dobles aperturas.
        // string appUrl = $"vnd.youtube:{item.YoutubeId}";

        Debug.Log($"[Select] Abriendo YouTube 360: {webUrl}");
        Application.OpenURL(webUrl);
    }
}
