using UnityEngine;

public class VideoGridController : MonoBehaviour
{
    [Header("Data")]
    public VideoList videoList;

    [Header("UI")]
    public Transform gridParent;          // Panel con GridLayoutGroup
    public GameObject videoTilePrefab;    // Prefab del tile

    // Paquete YouTube VR para Quest (según ADB en tu dispositivo)
    private const string YouTubeVrOculusPkg = "com.google.android.apps.youtube.vr.oculus";

    private void Start()
    {
        BuildGrid();
    }

    private void OpenBrowserFallback(string youtubeId)
{
    string url = string.IsNullOrEmpty(youtubeId)
        ? "https://www.youtube.com"
        : $"https://www.youtube.com/watch?v={youtubeId}";

    Debug.Log($"[YouTubeVR] Fallback → Abriendo en navegador: {url}");
    Application.OpenURL(url);
}

    public void BuildGrid()
    {
        if (!videoList)  { Debug.LogError("[VideoGridController] Falta VideoList"); return; }
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
            Debug.LogWarning("[Select] VideoItem inválido o sin YoutubeId.");
            return;
        }

        Debug.Log($"[Select] {item.Title} (YouTube ID: {item.YoutubeId})");
        OpenYoutubeApp(item.YoutubeId);
    }

    /// <summary>
    /// Abre el video en la app de YouTube VR para Quest usando intent nativo.
    /// En Editor/PC solo se hace log (no se abre navegador para respetar el requerimiento).
    /// </summary>
    private void OpenYoutubeApp(string youtubeId)
{
#if UNITY_ANDROID && !UNITY_EDITOR
    try
    {
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        using (var pm = currentActivity.Call<AndroidJavaObject>("getPackageManager"))
        {
            const string youtubeVrPackage = "com.google.android.apps.youtube.vr.oculus";

            // 1) Intent principal de la app (como si la lanzaras desde el menú del Quest)
            var launchIntent = pm.Call<AndroidJavaObject>(
                "getLaunchIntentForPackage",
                youtubeVrPackage
            );

            if (launchIntent == null)
            {
                Debug.LogWarning($"[YouTubeVR] No hay launch intent para {youtubeVrPackage}. Abriendo en navegador como fallback.");
                OpenBrowserFallback(youtubeId);
                return;
            }

            // 2) Opcional: pasarle la URL del video como data
            if (!string.IsNullOrEmpty(youtubeId))
            {
                string url = $"https://www.youtube.com/watch?v={youtubeId}";
                using (var uriClass = new AndroidJavaClass("android.net.Uri"))
                {
                    var uri = uriClass.CallStatic<AndroidJavaObject>("parse", url);
                    launchIntent.Call<AndroidJavaObject>("setData", uri);
                }
            }

            // Necesario cuando lanzas desde una Activity de Unity
            const int FLAG_ACTIVITY_NEW_TASK = 0x10000000;
            launchIntent.Call<AndroidJavaObject>("addFlags", FLAG_ACTIVITY_NEW_TASK);

            Debug.Log("[YouTubeVR] Lanzando YouTube VR con getLaunchIntentForPackage...");
            currentActivity.Call("startActivity", launchIntent);
        }
    }
    catch (AndroidJavaException e)
    {
        Debug.LogError("[YouTubeVR] Error al lanzar YouTube VR, usando fallback a navegador. Excepción: " + e);
        OpenBrowserFallback(youtubeId);
    }
#else
    OpenBrowserFallback(youtubeId);
#endif
    }
}
