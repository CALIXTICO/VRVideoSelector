using UnityEngine;
using UnityEngine.XR;

public class EyeGazeProvider : MonoBehaviour
{
    public enum Mode { Auto, HeadGazeOnly }

    [SerializeField] private Mode mode = Mode.Auto;
    [SerializeField] private bool log = true;

    private Camera cam;
    private InputDevice centerEye;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (!cam) cam = Camera.main;
    }

    void OnEnable()
    {
        centerEye = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
    }

    void OnDisable()
    {
        centerEye = default;
    }

    public bool TryGetEyeGazeRay(out Ray ray)
    {
        ray = default;

        if (mode == Mode.HeadGazeOnly)
            return TryHeadRay(out ray);

#if UNITY_OPENXR
        if (TryGetOpenXREyeRay(out ray))
            return true;
#endif
        // Fallback: head gaze
        return TryHeadRay(out ray);
    }

#if UNITY_OPENXR
    private bool TryGetOpenXREyeRay(out Ray ray)
    {
        ray = default;

        // Reintentar si el device se invalidó
        if (!centerEye.isValid)
            centerEye = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);

        if (!centerEye.isValid)
            return false;

        if (centerEye.TryGetFeatureValue(CommonUsages.eyesData, out Eyes eyes))
        {
            // 1) Fijación (fixationPoint)
            if (eyes.TryGetFixationPoint(out Vector3 fix))
            {
                var origin = cam ? cam.transform.position : Vector3.zero;
                var dir = fix - origin;
                if (dir.sqrMagnitude > 1e-6f)
                {
                    dir.Normalize();
                    ray = new Ray(origin, dir);
                    if (log) Debug.Log("[EyeGazeProvider] Using EyeGaze (fixationPoint).");
                    return true;
                }
            }

            // 2) Rotación de ojo (si no hay fixation)
            Quaternion leftRot = default, rightRot = default;
            bool hasLeft = eyes.TryGetLeftEyeRotation(out leftRot);
            bool hasRight = eyes.TryGetRightEyeRotation(out rightRot);

            if (hasLeft || hasRight)
            {
                var rot = hasRight ? rightRot : leftRot;
                var origin = cam ? cam.transform.position : Vector3.zero;

                // Si hay cámara, transformamos a su espacio
                Vector3 dir = cam
                    ? cam.transform.TransformDirection(rot * Vector3.forward)
                    : (rot * Vector3.forward);

                ray = new Ray(origin, dir);
                if (log) Debug.Log("[EyeGazeProvider] Using EyeGaze (eye rotation).");
                return true;
            }
        }

        return false;
    }
#endif

    private bool TryHeadRay(out Ray ray)
    {
        ray = default;
        if (!cam) cam = Camera.main;
        if (!cam) return false;

        ray = new Ray(cam.transform.position, cam.transform.forward);
        if (log) Debug.Log("[EyeGazeProvider] Fallback: HeadGaze");
        return true;
    }
}
