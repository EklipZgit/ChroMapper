using UnityEngine;
using UnityEngine.UI;

// I AM BECOMING THE JOKER AND REWRITING THIS SCRIPT TO ACTUALLY BATCH MULTIPLE ROUNDED CORNER IMAGES.
[ExecuteInEditMode]
public class ImageWithRoundedCorners : BaseMeshEffect
{
    public float radius;

    protected override void OnEnable()
    {
        base.OnEnable();
        Refresh();
    }

    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        Refresh();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        Refresh();
    }
#endif

    public void Refresh()
    {
        if (graphic != null)
        {
            graphic.SetVerticesDirty();
        }
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()) return;

        Rect rect = ((RectTransform)transform).rect;
        UIVertex vert = default;
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vert, i);
            vert.uv1 = new Vector4(rect.width, rect.height, radius, 0);
            vh.SetUIVertex(vert, i);
        }
    }
}
