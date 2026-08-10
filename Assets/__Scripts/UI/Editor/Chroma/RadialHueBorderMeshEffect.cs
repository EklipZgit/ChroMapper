using UnityEngine;
using UnityEngine.UI;

// Encode each selection image vertex's normalized tile position so the rainbow shader can form one centered hue wheel.
public class RadialHueBorderMeshEffect : BaseMeshEffect
{
    public override void ModifyMesh(VertexHelper vertexHelper)
    {
        if (!IsActive() || vertexHelper.currentVertCount == 0)
        {
            return;
        }

        var minimum = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        var maximum = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        for (var index = 0; index < vertexHelper.currentVertCount; index++)
        {
            var vertex = default(UIVertex);
            vertexHelper.PopulateUIVertex(ref vertex, index);
            minimum = Vector2.Min(minimum, vertex.position);
            maximum = Vector2.Max(maximum, vertex.position);
        }

        var size = maximum - minimum;
        if (Mathf.Approximately(size.x, 0f) || Mathf.Approximately(size.y, 0f))
        {
            return;
        }

        for (var index = 0; index < vertexHelper.currentVertCount; index++)
        {
            var vertex = default(UIVertex);
            vertexHelper.PopulateUIVertex(ref vertex, index);
            vertex.uv1 = new Vector2(
                (vertex.position.x - minimum.x) / size.x,
                (vertex.position.y - minimum.y) / size.y);
            vertexHelper.SetUIVertex(vertex, index);
        }
    }
}
