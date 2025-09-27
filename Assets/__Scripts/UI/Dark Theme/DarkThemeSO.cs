using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "DarkThemeSO", menuName = "Map/Dark Theme SO")]
public class DarkThemeSO : ScriptableObject
{
    [SerializeField] private Material beonMaterialReplacement;
    [SerializeField] public Material TekoMaterialReplacement;

    [SerializeField] private Font beonUnityReplacement;
    [SerializeField] private Font tekoUnityReplacement;

    public void DarkThemeifyUI()
    {
        if (!Settings.Instance.DarkTheme) return;
        foreach (var jankCodeMate in Resources.FindObjectsOfTypeAll<TextMeshProUGUI>())
        {
            if (jankCodeMate == null
                || jankCodeMate.font == null
                || (jankCodeMate.fontSharedMaterial != null && jankCodeMate.fontSharedMaterial.name.Contains("3D")))
                continue;

            if (jankCodeMate.font.name.Contains("Beon")) jankCodeMate.fontSharedMaterial = beonMaterialReplacement;
            if (jankCodeMate.font.name.Contains("Teko")) jankCodeMate.fontSharedMaterial = TekoMaterialReplacement;
        }

        foreach (var jankCodeMate in Resources.FindObjectsOfTypeAll<Text>())
        {
            if (jankCodeMate == null || jankCodeMate.font == null) continue;

            if (jankCodeMate.font.name.Contains("Beon")) jankCodeMate.font = beonUnityReplacement;
            if (jankCodeMate.font.name.Contains("Teko")) jankCodeMate.font = tekoUnityReplacement;
        }
    }
}
