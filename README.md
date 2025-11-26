# **Selector de videos de VR para Pacientes Paliativos controlado por la vista**
Tomás Bedoya C. - 202020689

## **Descripción General**
Este proyecto presenta una demo cuyo propósito es permitir que pacientes paliativos con restricciones de movilidad puedan facilmente elegir videos 360° de una galería previamente curada. La elección se hace con la mirada, aprovechando el rastreo de mirada de las Meta Quest Pro. La interacción está pensada para que, de un catalogo facilmente expandible, el paciente pueda mirar un video que llame su atención, sepa qué video está seleccionando con la mirada por medio de un aumento en el relieve, y pueda escogerlo si enfoca su mirada en ese mismo video por 5 segundos. La aplicación, entonces, despliega el video escogido en la aplicación de Youtube del visor, listo para experimentarse en 360° de modo immersivo.

El demo está desarrollado en Unity, y usa principalmente **OpenXR** como backend de XR y el **Meta XR All-in-One Utilities** (OVR/OVREyeGaze) para obtener el ray de mirada. También, para sistemas operativos que lo permiten, lanza los videos en la app de **YouTube VR para Quest** (con fallback al navegador).

> Plataforma objetivo: **Android / Meta Quest Pro**  
> Interacción principal: **eye-tracking + dwell (mirar X segundos)**

---

## Características

- **Selección por mirada pura**  
  - Ray de mirada obtenido desde `OVREyeGaze` (Meta XR SDK) mediante `EyeGazeProvider`.
  - No usa controladores, XR Ray Interactor ni InputDevices genéricos para el UI.

- **Raycaster de UI específico para eye-tracking**  
  - `EyeGazeUIRaycaster` convierte el ray 3D de la mirada en coordenadas de pantalla y hace `GraphicRaycaster` sobre un **Canvas en World Space**.
  - Fallback opcional a `Physics.Raycast` con `BoxCollider` en cada tile (via `TileColliderSync`), por si el UI cambió de configuración.

- **Grid dinámico de videos (fácil de extender)**  
  - Lista de videos centralizada en un `ScriptableObject` (`VideoList`).
  - Cada entrada (`VideoItem`) tiene:
    - `Title` (texto para UI)
    - `YoutubeId` (ID del video, p. ej. `G5Y_X9VeNrw`)
  - El controlador `VideoGridController` instancia automáticamente el prefab `VideoTile` para cada ítem de la lista.

- **Selección por dwell con feedback visual**  
  - Cada tile (`VideoTile`):
    - Muestra una miniatura (`RawImage`).
    - Tiene un anillo de progreso radial (`Image` tipo *Filled / Radial360*).
    - Usa un patrón de **pre-hold + fill**:
      - `preHoldTime`: tiempo mínimo mirando antes de mostrar el anillo.
      - `fillTime`: tiempo que tarda en llenarse el anillo hasta la selección.
  - Logs de tiempo de observación:
    - Tiempo total observado por tile.
    - Tiempo del dwell actual, logueado cada 1 segundo (`[Dwell] ...` en la consola).

- **Highlight visual al mirar**  
  - `GazeHighlight`: pequeña animación (escalado y tint de color) cuando el puntero de UI entra/sale sobre el tile.

- **Lanzamiento en YouTube VR (Android)**  
  - `VideoGridController` intenta lanzar el paquete:
    - `com.google.android.apps.youtube.vr.oculus`
  - Si no está disponible o falla el intent:
    - Fallback a navegador: abre `https://www.youtube.com/watch?v=<YouTubeId>` con `Application.OpenURL`. Deoendiendo del OS, Meta tiene bloqueadas las intents a aplicaciones desarrolladas por terceros, como Youtube, por lo que es comun que el fallback ocurra.

- **Permisos y prueba rápida de eye-tracking**  
  - `EyeTrackingPermissionAndProbe`:
    - Pide el permiso Android `com.oculus.permission.EYE_TRACKING`.
    - Comprueba que exista un dispositivo con `InputDeviceCharacteristics.EyeTracking`.
    - Intenta leer `eyesData` (fixation point) y loguea el estado.
  - `EyeProbe` (más minimalista): muestra en consola la disponibilidad del dispositivo y algunos valores de pose de gaze.

---

## Estructura del proyecto

Carpetas relevantes dentro de `Assets/`:

- `Assets/Scenes/`
  - `Main.unity` → escena principal del selector de videos.
  - `SampleScene.unity` → escena de ejemplo/ensayo (según configuración que elijas).

- `Assets/Data/`
  - `Video List.asset` → instancia del `ScriptableObject` `VideoList` con la lista de videos.

- `Assets/Scripts/`
  - **Eye tracking & raycasting**
    - `EyeGazeProvider.cs`  
      Envuelve `OVREyeGaze` (Meta SDK) y expone `TryGetEyeGazeRay(out Ray ray)`:
      - Verifica que el eye tracking esté habilitado (`EyeTrackingEnabled`).
      - Filtra por confianza mínima (`minConfidence`).
      - Si algo está mal, loguea un warning solo una vez.
    - `EyeGazeUIRaycaster.cs`  
      Usa `EyeGazeProvider` para:
      - Calcular un ray en 3D desde la mirada.
      - Intersectar ese ray con el plano del Canvas en World Space.
      - Traducir a coordenadas de pantalla para `GraphicRaycaster`.
      - Gestionar **selección única de un `VideoTile`**:
        - PointerEnter/Exit UI.
        - Llamadas a `BeginGaze()` / `EndGaze()` en la tile correspondiente.
      - Fallback opcional con `Physics.Raycast` sobre colliders de las tiles.
    - `EyeTrackingPermissionAndProbe.cs`  
      Solicita permiso de eye-tracking (Android) y prueba que haya datos válidos (`eyesData.fixationPoint`).
    - `EyeProbe.cs`  
      Probadito ligero con `InputDevices.GetDevicesWithCharacteristics(EyeTracking, ...)` para ver si llega información de mirada.

  - **UI y tiles**
    - `VideoItem.cs`  
      Clase serializable con:
      - `Title`
      - `YoutubeId`
      - `ThumbnailUrl` calculado (`https://img.youtube.com/vi/<id>/hqdefault.jpg`).
    - `VideoList.cs`  
      `ScriptableObject` con `List<VideoItem> Items`.  
      Incluye `OnValidate()` en Editor para limpiar espacios extra en títulos e IDs.
    - `VideoTile.cs`  
      Lógica de cada tile:
      - Asigna título + miniatura (`UnityWebRequestTexture.GetTexture` con fallback de URLs).
      - Configura el `Image` radial de progreso y asegura que no bloquee raycasts.
      - Implementa `Setup(VideoItem item, Action<VideoItem> onSelected)`.
      - Gestiona las corutinas:
        - `GazeSelectRoutine()` → pre-hold + fill del anillo, y al final llama al callback `onSelected`.
        - `DwellTickRoutine()` → acumula y loguea tiempo observado cada 1 s.
      - Implementa `BeginGaze()` / `EndGaze()` y `OnPointerEnter/Exit` para integrarse con `EventSystem`.
    - `VideoGridController.cs`  
      Controla el grid:
      - Recibe `VideoList`, `gridParent` y `videoTilePrefab`.
      - En `BuildGrid()` destruye tiles previas y crea una por cada `VideoItem`.
      - Para cada `VideoTile`, hace `Setup(item, OnVideoSelected)`.
      - En `OnVideoSelected(VideoItem item)`:
        - Loguea el título y YouTubeId.
        - Llama a `OpenYoutubeApp(youtubeId)` (intent Android).
        - Si falla o no hay YouTube VR → `OpenBrowserFallback(youtubeId)`.
    - `GazeDwellForwarder.cs`  
      Componente sencillo que implementa `IPointerEnter/ExitHandler` y llama directamente a `BeginGaze()` / `EndGaze()` de la `VideoTile`.
    - `GazeHighlight.cs`  
      Resalta visualmente el tile:
      - Escala (scaleUp) con interpolación suave.
      - Aplica un tint semitransparente.
      - Puede añadir dinámicamente un `Outline` (opcional).
    - `TileColliderSync.cs`  
      Sincroniza un `BoxCollider` con el tamaño de un `RectTransform`:
      - Convierte el tamaño en píxeles del Canvas a unidades de mundo.
      - Marca el collider como `isTrigger` para el `Physics.Raycast` del raycaster.

  - **XR**
    - `XRAutoStarter.cs`  
      Inicializa `XRGeneralSettings.Instance.Manager`:
      - `InitializeLoaderSync()`
      - `StartSubsystems()`
      - En `OnDestroy()`: `StopSubsystems()` + `DeinitializeLoader()`.

---

## Requisitos

- **Unity**  
  - Proyecto configurado con soporte para **Android** y **OpenXR**.
  - Paquetes típicos:
    - OpenXR Plugin para Unity.
    - Meta XR / Oculus Integration o **Meta XR All-in-One Utilities** (proporciona `OVREyeGaze` y assets de configuración).

- **Hardware**  
  - **Meta Quest Pro** con **eye-tracking activado** en la configuración del visor.

- **Software en el visor (opcional pero recomendado)**  
  - App **YouTube VR para Quest**:  
    - Paquete esperado: `com.google.android.apps.youtube.vr.oculus`.  
    - Si no está disponible, el demo abrirá el video en el navegador integrado.

---

## Cómo agregar / editar videos

Los videos viven en un `ScriptableObject`:

1. Ir a `Assets/Data/` y abrir `Video List.asset`.
2. En el inspector:
   - Agregar una nueva entrada a la lista `Items`.
   - Llenar:
     - **Title** → texto que se verá en la UI.
     - **YoutubeId** → ID del video de YouTube (la parte después de `watch?v=`).
3. Guardar la escena/proyecto.  
   - En runtime, `VideoGridController` reconstruirá la grilla con todos los elementos de `VideoList`.

> Si necesitas otra lista, puedes crear una nueva desde el menú:  
> `Create > Video Gallery > Video List` y asignarla al `VideoGridController`.

---

## Notas y posibles extensiones

- Actualmente el demo **no reproduce el video dentro de Unity**, sino que delega la reproducción a:
  - App de YouTube VR (si está instalada).
  - Navegador (fallback).

- El código de eye-tracking está separado en capas:
  - Capa Meta (`EyeGazeProvider` con `OVREyeGaze`).
  - Capa genérica de Input XR (`EyeTrackingPermissionAndProbe` / `EyeProbe` usando `InputDevices`).
  - Capa de UI (`EyeGazeUIRaycaster` + tiles). 
  - Esto facilita cambiar la fuente de gaze en el futuro (por ejemplo, usar otra implementación OpenXR) sin tocar la lógica de UI.

- Un APK listo para correr en las Quest Pro puede encontrarse en Builds/Android.

**Este commit comprente la primera versión exportable del proyecto visto en la reposición del martes 25 de noviembre.**
