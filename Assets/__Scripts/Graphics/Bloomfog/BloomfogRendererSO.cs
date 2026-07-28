using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(fileName = "BloomfogRendererSO", menuName = "Environment/BloomfogRendererSO")]
public class BloomfogRendererSO : ScriptableObject
{
    private static readonly int vertexTransformMatrix = Shader.PropertyToID("_VertexTransformMatrix");

    private const int startCapacity = 2048;

    private static BloomfogQuad[] bloomfogQuads = new BloomfogQuad[startCapacity];

    // Match Beat Saber BloomPrePassEffectSO's decompiled default bloom-prepass FOV. Probably this does nothing but matching BS decomp just in case. I don't see any obvious difference
    public Vector2 FOV = new(130f, 130f);
    public float LineWidth = 0.02f;
    public Material BloomfogObjectMaterial;

    private int capacity = startCapacity;
    private CommandBuffer bloomfogCommandBuffer;
    private Mesh bloomfogMesh;

    public void Initialize()
    {
        bloomfogCommandBuffer = new CommandBuffer() { name = "Bloomfog Render" };

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
        if (bloomfogCommandBuffer != null)
        {
            bloomfogCommandBuffer.Release();
            bloomfogCommandBuffer = null;
        }
    }

    public void RenderToTexture(Camera camera, RenderTexture tex, out Vector2 textureToScreenRatio)
    {
        if (bloomfogCommandBuffer == null || bloomfogMesh == null) Initialize();

        var viewMatrix = camera.worldToCameraMatrix;
        var projectionMatrix = camera.projectionMatrix;

        // Adjust projection matrix to account for FOV
        textureToScreenRatio.x = Mathf.Clamp01(1f / (Mathf.Tan(FOV.x * 0.5f * Mathf.Deg2Rad) * projectionMatrix.m00));
        textureToScreenRatio.y = Mathf.Clamp01(1f / (Mathf.Tan(FOV.y * 0.5f * Mathf.Deg2Rad) * projectionMatrix.m11));
        projectionMatrix.m00 *= textureToScreenRatio.x;
        projectionMatrix.m02 *= textureToScreenRatio.x;
        projectionMatrix.m11 *= textureToScreenRatio.y;
        projectionMatrix.m12 *= textureToScreenRatio.y;

        bloomfogCommandBuffer.Clear();
        bloomfogCommandBuffer.SetRenderTarget(tex);
        bloomfogCommandBuffer.ClearRenderTarget(true, true, Color.clear);

        RenderQuads(viewMatrix, projectionMatrix, LineWidth);

        bloomfogCommandBuffer.DrawMesh(bloomfogMesh, Matrix4x4.identity, BloomfogObjectMaterial);
    
        Graphics.ExecuteCommandBuffer(bloomfogCommandBuffer);
    }

    private void RenderQuads(Matrix4x4 view, Matrix4x4 projection, float lineWidth)
    {
        var lights = BloomFogObject.AllBloomFogLights;

        if (lights.Count > capacity) PrepareMesh();

        var activeLights = 0;
        for (var i = 0; i < lights.Count; i++)
        {
            lights[i].ApplyToQuad(ref activeLights, bloomfogQuads, view, projection, lineWidth);
        }

        var descriptor = new SubMeshDescriptor(0, activeLights * 6)
        {
            firstVertex = 0,
            vertexCount = activeLights * 4,
        };

        bloomfogMesh.SetVertexBufferData(bloomfogQuads, 0, 0, activeLights, 0, MeshUpdateFlags.DontRecalculateBounds);
        bloomfogMesh.subMeshCount = 1;
        bloomfogMesh.SetSubMesh(0, descriptor, MeshUpdateFlags.DontRecalculateBounds);
        bloomfogMesh.UploadMeshData(false);
    }

    private void PrepareMesh(bool force = false)
    {
        var lightCount = BloomFogObject.AllBloomFogLights.Count;

        if (!force && bloomfogMesh != null && capacity >= lightCount) return;

        while (capacity < lightCount)
        {
            capacity *= 2;
        }

        if (bloomfogMesh != null)
        {
            Debug.LogWarning("Need to recreate bloomfog mesh with larger capacity: " + capacity);
            bloomfogMesh.Clear();
        }
        else
        {
            Debug.Log("Generating bloomfog mesh with capacity: " + capacity);
            bloomfogMesh = new Mesh
            {
                name = "Bloomfog Mesh",
                indexFormat = IndexFormat.UInt32,
                vertexBufferTarget = GraphicsBuffer.Target.Vertex | GraphicsBuffer.Target.Raw
            };
        }

        // Initialize vertex buffer
        var vertexAttributes = new VertexAttributeDescriptor[]
        {
            new(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
            new(VertexAttribute.Tangent, VertexAttributeFormat.Float32, 3, 0),
            new(VertexAttribute.Color, VertexAttributeFormat.Float32, 4, 0),
            new(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 3, 0),
        };
        bloomfogMesh.SetVertexBufferParams(4 * capacity, vertexAttributes);

        // Recreate quad array (should be initialized to zeroes by default)
        bloomfogQuads = new BloomfogQuad[capacity];

        // Initialize index buffer
        var data = new NativeArray<ushort>(capacity * 6, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        for (var i = 0; i < capacity; i++)
        {
            data[i * 6] = (ushort)(i * 4);
            data[(i * 6) + 1] = (ushort)((i * 4) + 1);
            data[(i * 6) + 2] = (ushort)((i * 4) + 2);
            data[(i * 6) + 3] = (ushort)((i * 4) + 2);
            data[(i * 6) + 4] = (ushort)((i * 4) + 3);
            data[(i * 6) + 5] = (ushort)(i * 4);
        }
        bloomfogMesh.SetIndexBufferParams(data.Length, IndexFormat.UInt16);
        bloomfogMesh.SetIndexBufferData(data, 0, 0, data.Length, MeshUpdateFlags.Default);

        // Set submesh and bounds
        bloomfogMesh.subMeshCount = 1;
        bloomfogMesh.SetSubMesh(0, new SubMeshDescriptor(0, data.Length), MeshUpdateFlags.DontRecalculateBounds);
        bloomfogMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10000f);
        bloomfogMesh.UploadMeshData(false);
    }
}
