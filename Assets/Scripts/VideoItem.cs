using System;
using UnityEngine;

[Serializable]
public class VideoItem
{
    [Tooltip("Nombre legible para mostrar en la UI")]
    public string Title;

    [Tooltip("ID de YouTube (11 chars). Ej: G5Y_X9VeNrw")]
    public string YoutubeId;

    // Útil para la UI: URL directa al thumbnail de YouTube
    public string ThumbnailUrl => string.IsNullOrWhiteSpace(YoutubeId)
        ? null
        : $"https://img.youtube.com/vi/{YoutubeId}/hqdefault.jpg";
}
