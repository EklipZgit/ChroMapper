using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(fileName = "BloomfogRendererSO", menuName = "Environment/BloomfogRendererSO")]
public class BloomfogRendererSO : ScriptableObject
{
    private static readonly int vertexTransformMatrix = Shader.PropertyToID("_VertexTransformMatrix");

    private const int startCapacity = 1024;

    private static BloomfogQuad[] bloomfogQuads = new BloomfogQuad[startCapacity];

    public Vector2 FOV = new(90f, 90f);
    public float LineWidth = 0.02f;
    public Material BloomfogObjectMaterial;

    private int capacity = startCapacity;
    private Mesh bloomfogMesh;

    // We use a dedicated graphics buffer for colors because vertex colors are SDR only
    private GraphicsBuffer colorBuffer;
    private Color[] quadColors;

    public void Initialize()
    {
        PrepareMesh(true);
        Shader.SetGlobalMatrix(vertexTransformMatrix, Matrix4x4.Ortho(0, 1, 1, 0, -1, 1));
    }

    public void Release()
    {
        if (bloomfogMesh != null)
        {
            bloomfogMesh.Clear();
            DestroyImmediate(bloomfogMesh);
            bloomfogMesh = null;
        }
        if (colorBuffer != null)
        {
            colorBuffer.Dispose();
            colorBuffer = null;
        }
    }

    public void RenderToTexture(Camera camera, RenderTexture tex, out Vector2 textureToScreenRatio)
    {
        var viewMatrix = camera.worldToCameraMatrix;
        var projectionMatrix = camera.projectionMatrix;

        // Adjust projection matrix to account for FOV
        textureToScreenRatio.x = Mathf.Clamp01(1f / (Mathf.Tan(FOV.x * 0.5f * Mathf.Deg2Rad) * projectionMatrix.m00));
        textureToScreenRatio.y = Mathf.Clamp01(1f / (Mathf.Tan(FOV.y * 0.5f * Mathf.Deg2Rad) * projectionMatrix.m11));
        projectionMatrix.m00 *= textureToScreenRatio.x;
        projectionMatrix.m02 *= textureToScreenRatio.x;
        projectionMatrix.m11 *= textureToScreenRatio.y;
        projectionMatrix.m12 *= textureToScreenRatio.y;

        using var commandBuffer = new CommandBuffer() { name = "Bloomfog Render" };
        commandBuffer.SetRenderTarget(tex);
        commandBuffer.ClearRenderTarget(true, true, Color.clear);

        RenderQuads(viewMatrix, projectionMatrix, LineWidth);

        commandBuffer.DrawMesh(bloomfogMesh, Matrix4x4.identity, BloomfogObjectMaterial);
    
        Graphics.ExecuteCommandBuffer(commandBuffer);
    }

    private void RenderQuads(Matrix4x4 view, Matrix4x4 projection, float lineWidth)
    {
        if (bloomfogMesh == null) Initialize();

        var vertices = bloomfogMesh.vertices;
        var uvs = bloomfogMesh.uv;

        var lights = LightObjectBloomFog.AllBloomFogLights;

        if (lights.Count > capacity) PrepareMesh();

        for (var i = 0; i < lights.Count; i++)
        {
            lights[i].ApplyToQuad(i, bloomfogQuads, view, projection, lineWidth);

            ref var quad = ref bloomfogQuads[i];

            // Update mesh data
            var vertIndex = i * 4;

            vertices[vertIndex + 0] = quad.Vertex0Position;
            vertices[vertIndex + 1] = quad.Vertex1Position;
            vertices[vertIndex + 2] = quad.Vertex2Position;
            vertices[vertIndex + 3] = quad.Vertex3Position;

            uvs[vertIndex + 0] = quad.Vertex0UV;
            uvs[vertIndex + 1] = quad.Vertex1UV;
            uvs[vertIndex + 2] = quad.Vertex2UV;
            uvs[vertIndex + 3] = quad.Vertex3UV;

            quadColors[vertIndex + 0] = quad.Vertex0Color;
            quadColors[vertIndex + 1] = quad.Vertex1Color;
            quadColors[vertIndex + 2] = quad.Vertex2Color;
            quadColors[vertIndex + 3] = quad.Vertex3Color;
        }

        bloomfogMesh.vertices = vertices;
        bloomfogMesh.uv = uvs;
        bloomfogMesh.UploadMeshData(false);
        colorBuffer.SetData(quadColors);
    }

    private void PrepareMesh(bool force = false)
    {
        var lightCount = LightObjectBloomFog.AllBloomFogLights.Count;

        if (!force && bloomfogMesh != null && capacity >= lightCount) return;

        while (capacity < lightCount)
        {
            capacity *= 2;
        }

        if (bloomfogMesh != null)
        {
            Debug.LogWarning("Need to recreate bloomfog mesh with larger capacity: " + capacity);
            bloomfogMesh.Clear();
            colorBuffer.Dispose();
        }
        else
        {
            Debug.Log("Generating bloomfog mesh with capacity: " + capacity);
            bloomfogMesh = new Mesh
            {
                name = "Bloomfog Mesh",
                indexFormat = IndexFormat.UInt32,
            };
        }

        // Recreate quad array (should be initialized to zeroes by default)
        bloomfogQuads = new BloomfogQuad[capacity];
        colorBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, capacity * 4, sizeof(float) * 4);
        Shader.SetGlobalBuffer("_BloomfogColorBuffer", colorBuffer);

        var vertices = new Vector3[capacity * 4];
        var triangles = new int[capacity * 6];
        var uvs = new Vector2[capacity * 4];
        quadColors = new Color[capacity * 4];

        for (var i = 0; i < capacity; i++)
        {
            // 4 vertices per quad
            var vIndex = i * 4;
            vertices[vIndex + 0] = Vector3.zero;
            vertices[vIndex + 1] = Vector3.zero;
            vertices[vIndex + 2] = Vector3.zero;
            vertices[vIndex + 3] = Vector3.zero;

            // 6 indices per quad
            var tIndex = i * 6;
            triangles[tIndex + 0] = vIndex + 0;
            triangles[tIndex + 1] = vIndex + 1;
            triangles[tIndex + 2] = vIndex + 2;
            triangles[tIndex + 3] = vIndex + 2;
            triangles[tIndex + 4] = vIndex + 1;
            triangles[tIndex + 5] = vIndex + 3;
        }

        bloomfogMesh.vertices = vertices;
        bloomfogMesh.triangles = triangles;
        bloomfogMesh.uv = uvs;
        colorBuffer.SetData(quadColors);
    }
}
