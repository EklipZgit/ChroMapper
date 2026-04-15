using UnityEditor;
using UnityEngine;

public class VectorShowIfAnyDrawer : ShowIfAnyDrawer
{
    private readonly int dimensions;

    public VectorShowIfAnyDrawer(float dims) => dimensions = (int)dims;
    public VectorShowIfAnyDrawer(float dims, params string[] keywords) : base(keywords) => dimensions = (int)dims;

    public VectorShowIfAnyDrawer(float dims, float required, params string[] keywords) : base(required, keywords) =>
        dimensions = (int)dims;

    public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
    {
        if (!IsVisible(prop)) return;

        var val = prop.vectorValue;

        EditorGUI.BeginChangeCheck();
        switch (dimensions)
        {
            case 2:
                {
                    var v2 = EditorGUI.Vector2Field(position, label, new Vector2(val.x, val.y));
                    if (EditorGUI.EndChangeCheck() && IsVisible(prop))
                        prop.vectorValue = new Vector4(v2.x, v2.y, val.z, val.w);
                    break;
                }
            case 3:
                {
                    var v3 = EditorGUI.Vector3Field(position, label, new Vector3(val.x, val.y, val.z));
                    if (EditorGUI.EndChangeCheck() && IsVisible(prop))
                        prop.vectorValue = new Vector4(v3.x, v3.y, v3.z, val.w);
                    break;
                }
            default:
                editor.DefaultShaderProperty(position, prop, label);
                break;
        }
    }
}
