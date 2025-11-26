using UnityEngine;

/// <summary>
/// Proveedor de ray de mirada usando directamente el Meta SDK (OVREyeGaze),
/// sin usar UnityEngine.XR.InputDevices ni la capa genérica de OpenXR.
/// </summary>
public class EyeGazeProvider : MonoBehaviour
{
    [Header("Fuente de gaze del Meta SDK")]
    [Tooltip("OVREyeGaze que cuelga de la cámara (EyeGazeAnchor).")]
    [SerializeField] private OVREyeGaze eyeGaze;

    [Header("Filtros de calidad")]
    [Range(0f, 1f)]
    [Tooltip("Confianza mínima para aceptar una muestra de gaze.")]
    [SerializeField] private float minConfidence = 0.6f;

    private bool loggedProblem;

    /// <summary>
    /// Devuelve true si hay un ray de mirada válido.
    /// </summary>
    public bool TryGetEyeGazeRay(out Ray ray)
    {
        ray = default;

        if (eyeGaze == null)
        {
            if (!loggedProblem)
            {
                Debug.LogWarning("[EyeGazeProvider] Falta referencia a OVREyeGaze en el inspector.");
                loggedProblem = true;
            }
            return false;
        }

        // Propiedad del Meta SDK: true cuando el visor soporta eye tracking y el usuario lo ha permitido.
        if (!eyeGaze.EyeTrackingEnabled)
        {
            if (!loggedProblem)
            {
                Debug.LogWarning("[EyeGazeProvider] EyeTrackingEnabled = false (revisa permisos del app en el visor y que el eye tracking esté activado).");
                loggedProblem = true;
            }
            return false;
        }

        // Confidence es 0..1, cuánta confianza tiene el SDK en la lectura actual
        if (eyeGaze.Confidence < minConfidence)
        {
            // Esto pasa a veces cuando el usuario parpadea, mira fuera del FOV, etc.
            return false;
        }

        // Si llegamos aquí, tenemos dato bueno
        loggedProblem = false;

        var origin = eyeGaze.transform.position;
        var direction = eyeGaze.transform.forward;

        ray = new Ray(origin, direction);
        return true;
    }

    private void Reset()
    {
        // Si no se asigna a mano en el inspector, intenta encontrar uno en hijos.
        if (eyeGaze == null)
        {
            eyeGaze = GetComponentInChildren<OVREyeGaze>();
        }
    }
}
