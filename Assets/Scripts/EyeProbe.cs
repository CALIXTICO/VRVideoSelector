using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

public class EyeProbe : MonoBehaviour
{
    InputDevice eyeDevice;
    List<InputDevice> devices = new List<InputDevice>();

    void GetEyeDevice()
    {
        devices.Clear();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.EyeTracking, devices);

        if (devices.Count == 0)
        {
            Debug.Log("[EyeProbe] No eye tracking devices found");
            return;
        }

        eyeDevice = devices[0];
        Debug.Log($"[EyeProbe] Using device: {eyeDevice.name}, ch: {eyeDevice.characteristics}");
    }

    void Update()
    {
        if (!eyeDevice.isValid)
        {
            GetEyeDevice();
            if (!eyeDevice.isValid)
            {
                // Sigue sin haber device de ojos
                return;
            }
        }

        // Probar EyesData
        if (eyeDevice.TryGetFeatureValue(CommonUsages.eyesData, out Eyes eyes))
        {
            if (eyes.TryGetFixationPoint(out Vector3 fixation))
            {
                Debug.Log($"[EyeProbe] Fixation: {fixation}");
            }
            else
            {
                Debug.Log("[EyeProbe] eyesData OK pero sin fixation");
            }
        }
        else
        {
            Debug.Log("[EyeProbe] NO eyesData (TryGetFeatureValue falló)");
        }

        // Probar también posición/rotación del dispositivo
        bool hasPos = eyeDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos);
        bool hasRot = eyeDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot);

        if (hasPos && hasRot)
        {
            Debug.Log($"[EyeProbe] Gaze pose: {pos} / {rot}");
        }
        else
        {
            Debug.Log("[EyeProbe] NO devicePosition/Rotation");
        }
    }
}
