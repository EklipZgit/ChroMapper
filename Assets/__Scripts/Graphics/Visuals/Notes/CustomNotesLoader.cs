using System.IO;
using System.Linq;
using UnityEngine;

public class CustomNotesLoader : MonoBehaviour
{
    public VisualRepositorySO Repository;

    public void Start()
    {
        var customNotePath = PathUtils.Combine(Settings.Instance.BeatSaberInstallation, "CustomNotes");
        if (!Directory.Exists(customNotePath)) return;

        foreach (var filePath in Directory
            .EnumerateFiles(
                customNotePath,
                "*",
                SearchOption.TopDirectoryOnly)
            .Where(f => f.EndsWith(".bloq") || f.EndsWith(".note")))
        { 
            var assetBundle = AssetBundle.LoadFromFile(filePath);
            Repository.Add(NoteModelSO.Create(assetBundle));
        }
    }
}
