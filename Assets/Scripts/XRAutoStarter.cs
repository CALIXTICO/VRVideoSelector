using System.Collections;
using UnityEngine;
using UnityEngine.XR.Management;

public class XRAutoStarter : MonoBehaviour
{
    private IEnumerator Start()
    {
        var xrSettings = XRGeneralSettings.Instance;
        if (xrSettings == null)
        {
            Debug.LogError("[XRAutoStarter] XRGeneralSettings.Instance es null");
            yield break;
        }

        var xrManager = xrSettings.Manager;
        if (xrManager == null)
        {
            Debug.LogError("[XRAutoStarter] XR Manager es null");
            yield break;
        }

        // Inicializar loader si aún no está activo
        if (xrManager.activeLoader == null)
        {
            Debug.Log("[XRAutoStarter] Inicializando XR Loader...");
#if UNITY_2020_2_OR_NEWER
            yield return xrManager.InitializeLoader();
#else
            xrManager.InitializeLoaderSync();
#endif
        }

        if (xrManager.activeLoader == null)
        {
            Debug.LogError("[XRAutoStarter] No se pudo inicializar ningún XR Loader");
            yield break;
        }

        Debug.Log("[XRAutoStarter] XR Loader activo, arrancando subsistemas...");
        xrManager.StartSubsystems();
    }

    private void OnDestroy()
    {
        var xrSettings = XRGeneralSettings.Instance;
        if (xrSettings == null) return;

        var xrManager = xrSettings.Manager;
        if (xrManager == null) return;

        Debug.Log("[XRAutoStarter] Parando subsistemas XR...");
        xrManager.StopSubsystems();
        xrManager.DeinitializeLoader();
    }
}
