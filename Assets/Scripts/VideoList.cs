using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Video List",
    menuName = "Video Gallery/Video List",
    order = 0)]
public class VideoList : ScriptableObject
{
    public List<VideoItem> Items = new List<VideoItem>();

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Limpieza ligera para evitar espacios accidentales
        if (Items == null) return;
        foreach (var it in Items)
        {
            if (it == null) continue;
            if (!string.IsNullOrEmpty(it.Title)) it.Title = it.Title.Trim();
            if (!string.IsNullOrEmpty(it.YoutubeId)) it.YoutubeId = it.YoutubeId.Trim();
        }
    }
#endif
}
