using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ShowIfAnyDrawer : MaterialPropertyDrawer
{
    private readonly string[] requiredKeywords;
    private readonly string[] anyKeywords;

    public ShowIfAnyDrawer()
    {
        requiredKeywords = Array.Empty<string>();
        anyKeywords = Array.Empty<string>();
    }

    public ShowIfAnyDrawer(params string[] keywords)
    {
        requiredKeywords = Array.Empty<string>();
        anyKeywords = keywords;
    }

    public ShowIfAnyDrawer(float required, params string[] keywords)
    {
        requiredKeywords = keywords.Take((int)required).ToArray();
        anyKeywords = keywords.Skip((int)required).ToArray();
    }

    protected bool IsVisible(MaterialProperty prop)
    {
        if (requiredKeywords.Length == 0 && anyKeywords.Length == 0) return true;

        foreach (var obj in prop.targets)
        {
            var mat = obj as Material;
            if (mat == null) return false;
            return (requiredKeywords.Length == 0 || requiredKeywords.All(ConditionalKeyword))
                && (anyKeywords.Length == 0 || anyKeywords.Any(ConditionalKeyword));

            bool ConditionalKeyword(string keyword)
            {
                if (keyword.StartsWith('0'))
                {
                    var revKeyword = keyword[1..];
                    // var count = mat.shader.GetPropertyCount();
                    // for (var i = 0; i < count; i++)
                    // {
                    //     var attributes = mat.shader.GetPropertyAttributes(i);
                    //     var propName = mat.shader.GetPropertyName(i).ToUpper();
                    //     if (revKeyword.StartsWith(propName, StringComparison.OrdinalIgnoreCase)) return true;
                    // }

                    return !mat.IsKeywordEnabled(revKeyword);
                }

                return mat.IsKeywordEnabled(keyword);
            }
        }

        return false;
    }

    public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
    {
        if (IsVisible(prop)) editor.DefaultShaderProperty(position, prop, label);
    }

    public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
    {
        return prop.type switch
        {
            MaterialProperty.PropType.Vector => IsVisible(prop)
                ? EditorGUIUtility.wideMode
                    ? base.GetPropertyHeight(prop, label, editor)
                    : EditorGUIUtility.singleLineHeight * 2f
                : -2f,
            MaterialProperty.PropType.Texture => IsVisible(prop) ? EditorGUIUtility.singleLineHeight * 4f : -2f,
            _ => IsVisible(prop) ? base.GetPropertyHeight(prop, label, editor) : -2f
        };
    }
}
