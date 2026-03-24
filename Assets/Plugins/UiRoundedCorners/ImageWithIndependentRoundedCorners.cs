using UnityEngine;
using UnityEngine.UI;

// I AM BECOMING THE JOKER AND REWRITING THIS SCRIPT TO ACTUALLY BATCH MULTIPLE ROUNDED CORNER IMAGES.
[ExecuteInEditMode]
public class ImageWithIndependentRoundedCorners : BaseMeshEffect
{
    public Vector4 r;

    // xy - position,
    // zw - halfSize
    [HideInInspector, SerializeField] private Vector4 rect2props;

    // Vector2.right rotated clockwise by 45 degrees
    private static readonly Vector2 wNorm = new(.7071068f, -.7071068f);
    // Vector2.right rotated counter-clockwise by 45 degrees
    private static readonly Vector2 hNorm = new(.7071068f, .7071068f);

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
        RecalculateProps(rect.size);

        Vector2 halfSize = rect.size * .5f;

        UIVertex vert = default;
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vert, i);
            // uv1: (r.x, r.y, r.z, r.w)
            vert.uv1 = r;
            // uv2: (halfSize.x, halfSize.y, rect2props.x, rect2props.y)
            vert.uv2 = new Vector4(halfSize.x, halfSize.y, rect2props.x, rect2props.y);
            // uv3: (rect2props.z, rect2props.w, 0, 0)
            vert.uv3 = new Vector4(rect2props.z, rect2props.w, 0, 0);
            vh.SetUIVertex(vert, i);
        }
    }

    private void RecalculateProps(Vector2 size)
    {

        // Vector that goes from left to right sides of rect2
        Vector2 aVec = new Vector2(size.x, -size.y + r.x + r.z);

        // Project vector aVec to wNorm to get magnitude of rect2 width vector
        float halfWidth = Vector2.Dot(aVec, wNorm) * .5f;
        rect2props.z = halfWidth;


        // Vector that goes from bottom to top sides of rect2
        Vector2 bVec = new Vector2(size.x, size.y - r.w - r.y);

        // Project vector bVec to hNorm to get magnitude of rect2 height vector
        float halfHeight = Vector2.Dot(bVec, hNorm) * .5f;
        rect2props.w = halfHeight;


        // Vector that goes from left to top sides of rect2
        Vector2 efVec = new Vector2(size.x - r.x - r.y, 0);
        // Vector that goes from point E to point G, which is top-left of rect2
        Vector2 egVec = hNorm * Vector2.Dot(efVec, hNorm);
        // Position of point E relative to center of coord system
        Vector2 ePoint = new Vector2(r.x - (size.x / 2), size.y / 2);
        // Origin of rect2 relative to center of coord system
        // ePoint + egVec == vector to top-left corner of rect2
        // wNorm * halfWidth + hNorm * -halfHeight == vector from top-left corner to center
        Vector2 origin = ePoint + egVec + wNorm * halfWidth + hNorm * -halfHeight;
        rect2props.x = origin.x;
        rect2props.y = origin.y;
    }
}
