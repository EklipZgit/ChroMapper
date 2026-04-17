using System.Linq;
using UnityEditor;
using UnityEngine;

public class EnumShowIfAnyDrawer : ShowIfAnyDrawer
{
    private readonly string[] options;

    public EnumShowIfAnyDrawer(float optionCount, params string[] keywords) :
        base(keywords.Skip((int)optionCount).ToArray()) =>
        options = keywords.Take((int)optionCount).ToArray();

    public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
    {
        if (!IsVisible(prop)) return;

        EditorGUI.BeginChangeCheck();

        var index = (int)prop.floatValue;
        index = EditorGUI.Popup(position, label, index, options);


        prop.floatValue = index;
        SetKeywords(prop, index);
        if (!EditorGUI.EndChangeCheck()) return;
        
    }

    private void SetKeywords(MaterialProperty prop, int index)
    {
        foreach (var target in prop.targets)
        {
            var mat = (Material)target;
            for (var i = 0; i < options.Length; i++)
            {
                var keyword = (prop.name + "_" + options[i]).ToUpper();
                if (i == index)
                    mat.EnableKeyword(keyword);
                else
                    mat.DisableKeyword(keyword);
            }
        }
    }
}
