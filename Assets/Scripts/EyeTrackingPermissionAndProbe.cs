// Assets/Scripts/EyeTrackingPermissionAndProbe.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class EyeTrackingPermissionAndProbe : MonoBehaviour
{
#if UNITY_ANDROID && !UNITY_EDITOR
    const string EyePerm = "com.oculus.permission.EYE_TRACKING";
#endif

    IEnumerator Start()
    {
        // 1) Pedir permiso en Android
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(EyePerm))
        {
            UnityEngine.Android.Permission.RequestUserPermission(EyePerm);
            // Espera 1 frame y re-chequea
            yield return null;
        }
#endif
        yield return new WaitForSeconds(0.25f);

        // 2) Probar si hay dispositivo de Eye Tracking y datos
        var eyesDevices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.EyeTracking, eyesDevices);

        if (eyesDevices.Count == 0)
        {
            Debug.LogWarning("[EyeProbe] No hay dispositivo de EyeTracking (revisa OpenXR Eye Gaze + permiso + calibración).");
            yield break;
        }

        Debug.Log($"[EyeProbe] Dispositivos ojos: {eyesDevices.Count}");

        // 3) Intentar leer FixationPoint / direcciones
        foreach (var dev in eyesDevices)
        {
            if (dev.TryGetFeatureValue(CommonUsages.eyesData, out Eyes eyes))
            {
                if (eyes.TryGetFixationPoint(out Vector3 fixation))
                {
                    Debug.Log($"[EyeProbe] FixationPoint: {fixation}");
                }
                else
                {
                    Debug.LogWarning("[EyeProbe] Sin FixationPoint todavía (mira algo y asegúrate de estar en app 3D).");
                }
            }
            else
            {
                Debug.LogWarning("[EyeProbe] Este device no entregó CommonUsages.eyesData.");
            }
        }
    }
}
