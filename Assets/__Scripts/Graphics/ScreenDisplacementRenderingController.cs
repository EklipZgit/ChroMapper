using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

// After transparent rendering, copies the camera color into a temporary source and
// redraws obstacle cores and foreground sprites in their native sorting order.
public sealed class ScreenDisplacementRenderingController : MonoBehaviour
{
    private const int maxInstancedRenderers = 1023;
    private const CameraEvent displacementCameraEvent = CameraEvent.AfterForwardAlpha;

    private static readonly int grabTextureId = Shader.PropertyToID("_ScreenDisplacementGrabTexture");
    private static readonly int grabTextureTexelSizeId =
        Shader.PropertyToID("_ScreenDisplacementGrabTexture_TexelSize");
    [SerializeField, Range(0, 31)] private int displacementLayer = 31;

    private readonly List<RendererSortData> sortedRenderers = new();
    private readonly Plane[] frustumPlanes = new Plane[6];

    private struct RendererSortData
    {
        public ScreenDisplacementRenderer Component;
        public Material Material;
        public int SortingLayerId;
        public int SortingLayerValue;
        public int SortingOrder;
        public int RenderQueue;
        public float Depth;
    }
    private static readonly IComparer<RendererSortData> rendererComparer =
        Comparer<RendererSortData>.Create(CompareRenderers);
    private readonly Matrix4x4[] instanceMatrices = new Matrix4x4[maxInstancedRenderers];
    private readonly Vector4[] instanceColors = new Vector4[maxInstancedRenderers];
    private readonly Vector4[] instanceTintColors = new Vector4[maxInstancedRenderers];
    private readonly Vector4[] instanceAddColors = new Vector4[maxInstancedRenderers];
    private readonly float[] instanceCutouts = new float[maxInstancedRenderers];
    private readonly Vector4[] instanceCutoutTexOffsets = new Vector4[maxInstancedRenderers];
    private readonly Vector4[] instanceUvScales = new Vector4[maxInstancedRenderers];
    private MaterialPropertyBlock sourcePropertyBlock;
    private MaterialPropertyBlock instancedPropertyBlock;

    private static readonly int colorId = Shader.PropertyToID("_Color");
    private static readonly int tintColorId = Shader.PropertyToID("_TintColor");
    private static readonly int addColorId = Shader.PropertyToID("_AddColor");
    private static readonly int cutoutId = Shader.PropertyToID("_Cutout");
    private static readonly int cutoutTexOffsetId = Shader.PropertyToID("_CutoutTexOffset");
    private static readonly int uvScaleId = Shader.PropertyToID("_UVScale");

    private Camera activeCamera;
    private CommandBuffer commandBuffer;
    private int previousDisplacementLayerMask;
    private bool active;
    private bool commandBufferAttached;
    private bool screenDisplacementEnabled;
    private bool settingsCallbackSubscribed;

    public void AssignToCamera(CameraController cameraController)
    {
        DetachCommandBuffer();
        activeCamera = cameraController == null ? null : cameraController.Camera;
        AttachCommandBuffer();
    }

    private void OnEnable()
    {
        UpdateScreenDisplacement(Settings.Instance.ScreenDisplacement);
        if (!settingsCallbackSubscribed)
        {
            Settings.NotifyBySettingName(nameof(Settings.ScreenDisplacement), UpdateScreenDisplacement);
            settingsCallbackSubscribed = true;
        }
        if (screenDisplacementEnabled) Activate();
    }

    private void OnDisable()
    {
        if (settingsCallbackSubscribed)
        {
            Settings.StopNotifyingBySettingName(
                nameof(Settings.ScreenDisplacement), UpdateScreenDisplacement);
            settingsCallbackSubscribed = false;
        }
        Deactivate();
        ScreenDisplacementRenderer.SetEnabled(false);
    }

    private void OnDestroy()
    {
        OnDisable();
        Deactivate();
        if (commandBuffer != null)
        {
            commandBuffer.Release();
            commandBuffer = null;
        }
    }

    private void Activate()
    {
        if (active || !screenDisplacementEnabled) return;
        active = true;
        ScreenDisplacementRenderer.SetEnabled(true);
        Camera.onPreRender += OnCameraPreRender;
        AttachCommandBuffer();
    }

    private void Deactivate()
    {
        if (active)
        {
            active = false;
            Camera.onPreRender -= OnCameraPreRender;
        }
        DetachCommandBuffer();
        ScreenDisplacementRenderer.SetEnabled(false);
    }

    private void AttachCommandBuffer()
    {
        if (!active || activeCamera == null || commandBufferAttached) return;

        commandBuffer ??= new CommandBuffer { name = "ChroMapper Screen Displacement" };
        var layerBit = 1 << displacementLayer;
        // The command buffer becomes the sole draw path for the displacement layer.
        previousDisplacementLayerMask = activeCamera.cullingMask & layerBit;
        activeCamera.cullingMask &= ~layerBit;
        activeCamera.AddCommandBuffer(displacementCameraEvent, commandBuffer);
        commandBufferAttached = true;
    }

    private void DetachCommandBuffer()
    {
        if (!commandBufferAttached) return;

        if (activeCamera != null)
        {
            activeCamera.RemoveCommandBuffer(displacementCameraEvent, commandBuffer);
            var layerBit = 1 << displacementLayer;
            activeCamera.cullingMask =
                (activeCamera.cullingMask & ~layerBit) | previousDisplacementLayerMask;
        }

        commandBufferAttached = false;
        commandBuffer?.Clear();
        Shader.SetGlobalTexture(grabTextureId, null);
        Shader.SetGlobalVector(grabTextureTexelSizeId, Vector4.zero);
    }

    private void OnCameraPreRender(Camera renderingCamera)
    {
        if (renderingCamera != activeCamera || commandBuffer == null) return;

        commandBuffer.Clear();
        var requiresScreenCopy = CollectRenderers();
        if (sortedRenderers.Count == 0) return;

        var cameraTarget = new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);

        // Sorting-order-1 spawn sprites are excluded from this copy and drawn after the cores.
        // A frame containing only those sprites needs no grab texture.
        if (requiresScreenCopy)
        {
            var grabDescriptor = GetGrabDescriptor();
            commandBuffer.GetTemporaryRT(grabTextureId, grabDescriptor, FilterMode.Bilinear);
            commandBuffer.Blit(cameraTarget, grabTextureId);
            commandBuffer.SetGlobalTexture(grabTextureId, grabTextureId);
            commandBuffer.SetGlobalVector(grabTextureTexelSizeId,
                new Vector4(1f / grabDescriptor.width, 1f / grabDescriptor.height,
                    grabDescriptor.width, grabDescriptor.height));
        }
        commandBuffer.SetRenderTarget(cameraTarget);

        DrawRenderers();

        if (requiresScreenCopy)
        {
            commandBuffer.SetGlobalTexture(grabTextureId, Texture2D.blackTexture);
            commandBuffer.SetGlobalVector(grabTextureTexelSizeId, Vector4.zero);
            commandBuffer.ReleaseTemporaryRT(grabTextureId);
        }
    }

    private void DrawRenderers()
    {
        // A script reload can preserve this component without rerunning field initializers.
        // Recreate native-backed blocks at their use boundary.
        sourcePropertyBlock ??= new MaterialPropertyBlock();
        instancedPropertyBlock ??= new MaterialPropertyBlock();

        var instanceCount = 0;
        Mesh batchMesh = null;
        Material batchMaterial = null;
        var batchSortingLayer = 0;
        var batchSortingOrder = 0;
        var batchRenderQueue = 0;

        foreach (var entry in sortedRenderers)
        {
            var renderer = entry.Component.TargetRenderer;
            var material = entry.Material;
            if (!TryGetInstancingMesh(entry.Component, material, out var mesh))
            {
                FlushInstancedBatch(ref instanceCount, ref batchMesh, ref batchMaterial);
                commandBuffer.DrawRenderer(renderer, material, 0, -1);
                continue;
            }

            var compatible = instanceCount == 0
                || (mesh == batchMesh
                    && material == batchMaterial
                    && entry.SortingLayerId == batchSortingLayer
                    && entry.SortingOrder == batchSortingOrder
                    && entry.RenderQueue == batchRenderQueue);
            if (!compatible)
            {
                FlushInstancedBatch(ref instanceCount, ref batchMesh, ref batchMaterial);
            }

            if (instanceCount == 0)
            {
                batchMesh = mesh;
                batchMaterial = material;
                batchSortingLayer = entry.SortingLayerId;
                batchSortingOrder = entry.SortingOrder;
                batchRenderQueue = entry.RenderQueue;
            }

            AddInstancedRenderer(renderer, material, instanceCount++);
            if (instanceCount == maxInstancedRenderers)
            {
                FlushInstancedBatch(ref instanceCount, ref batchMesh, ref batchMaterial);
            }
        }

        FlushInstancedBatch(ref instanceCount, ref batchMesh, ref batchMaterial);
    }

    private bool TryGetInstancingMesh(ScreenDisplacementRenderer component, Material material, out Mesh mesh)
    {
        mesh = null;
        if (!SystemInfo.supportsInstancing || material == null || !material.enableInstancing)
        {
            return false;
        }
        if (component.TargetRenderer is not MeshRenderer)
        {
            return false;
        }

        var meshFilter = component.MeshFilter;
        mesh = meshFilter == null ? null : meshFilter.sharedMesh;
        return mesh != null && mesh.subMeshCount > 0;
    }

    private void AddInstancedRenderer(Renderer renderer, Material material, int index)
    {
        if (index >= maxInstancedRenderers)
            throw new System.ArgumentOutOfRangeException(nameof(index));

        instanceMatrices[index] = renderer.transform.localToWorldMatrix;
        sourcePropertyBlock.Clear();
        renderer.GetPropertyBlock(sourcePropertyBlock);
        // Preserve the active-space RGB stored by SetColor when uploading raw vector arrays.
        instanceColors[index] = sourcePropertyBlock.HasProperty(colorId)
            ? sourcePropertyBlock.GetVector(colorId) : (Vector4)material.GetColor(colorId);
        instanceTintColors[index] = sourcePropertyBlock.HasProperty(tintColorId)
            ? sourcePropertyBlock.GetVector(tintColorId) : (Vector4)material.GetColor(tintColorId);
        instanceAddColors[index] = sourcePropertyBlock.HasProperty(addColorId)
            ? sourcePropertyBlock.GetVector(addColorId) : (Vector4)material.GetColor(addColorId);
        instanceCutouts[index] = sourcePropertyBlock.HasProperty(cutoutId)
            ? sourcePropertyBlock.GetFloat(cutoutId) : material.GetFloat(cutoutId);
        instanceCutoutTexOffsets[index] = sourcePropertyBlock.HasProperty(cutoutTexOffsetId)
            ? sourcePropertyBlock.GetVector(cutoutTexOffsetId) : material.GetVector(cutoutTexOffsetId);
        instanceUvScales[index] = sourcePropertyBlock.HasProperty(uvScaleId)
            ? sourcePropertyBlock.GetVector(uvScaleId) : material.GetVector(uvScaleId);

        // Carry per-renderer property-block values as arrays to retain instancing.
    }

    private void FlushInstancedBatch(
        ref int instanceCount, ref Mesh batchMesh, ref Material batchMaterial)
    {
        if (instanceCount == 0) return;
        if (instanceCount > maxInstancedRenderers)
            throw new System.InvalidOperationException("Screen displacement instance batch exceeds Unity's 1023 limit.");

        instancedPropertyBlock.Clear();
        instancedPropertyBlock.SetVectorArray(colorId, instanceColors);
        instancedPropertyBlock.SetVectorArray(tintColorId, instanceTintColors);
        instancedPropertyBlock.SetVectorArray(addColorId, instanceAddColors);
        instancedPropertyBlock.SetFloatArray(cutoutId, instanceCutouts);
        instancedPropertyBlock.SetVectorArray(cutoutTexOffsetId, instanceCutoutTexOffsets);
        instancedPropertyBlock.SetVectorArray(uvScaleId, instanceUvScales);
        commandBuffer.DrawMeshInstanced(
            batchMesh, 0, batchMaterial, -1, instanceMatrices, instanceCount, instancedPropertyBlock);
        instanceCount = 0;
        batchMesh = null;
        batchMaterial = null;
    }

    private bool CollectRenderers()
    {
        var requiresScreenCopy = false;
        sortedRenderers.Clear();
        GeometryUtility.CalculateFrustumPlanes(activeCamera, frustumPlanes);
        var cameraTransform = activeCamera.transform;
        var cameraPosition = cameraTransform.position;
        var cameraForward = cameraTransform.forward;
        foreach (var displacementRenderer in ScreenDisplacementRenderer.Renderers)
        {
            if (displacementRenderer != null && displacementRenderer.IsReady)
            {
                var renderer = displacementRenderer.TargetRenderer;
                var bounds = renderer.bounds;
                // Command-buffer draws bypass Unity's renderer culling. Offscreen walls need no
                // property-block copies, sorting, or instance uploads for this camera.
                if (!GeometryUtility.TestPlanesAABB(frustumPlanes, bounds))
                {
                    continue;
                }
                var material = renderer.sharedMaterial;
                requiresScreenCopy |= displacementRenderer.RequiresScreenCopy;
                var layerId = renderer.sortingLayerID;
                sortedRenderers.Add(new RendererSortData
                {
                    Component = displacementRenderer,
                    Material = material,
                    SortingLayerId = layerId,
                    SortingLayerValue = SortingLayer.GetLayerValueFromID(layerId),
                    SortingOrder = renderer.sortingOrder,
                    RenderQueue = material.renderQueue,
                    Depth = Vector3.Dot(bounds.center - cameraPosition, cameraForward)
                });
            }
        }

        sortedRenderers.Sort(rendererComparer);
        return requiresScreenCopy;
    }

    private static int CompareRenderers(RendererSortData left, RendererSortData right)
    {
        var comparison = left.SortingLayerValue.CompareTo(right.SortingLayerValue);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = left.SortingOrder.CompareTo(right.SortingOrder);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = left.RenderQueue.CompareTo(right.RenderQueue);
        if (comparison != 0)
        {
            return comparison;
        }

        return left.Depth.CompareTo(right.Depth);
    }

    private void UpdateScreenDisplacement(object value)
    {
        screenDisplacementEnabled = System.Convert.ToBoolean(value);
        if (screenDisplacementEnabled)
        {
            ScreenDisplacementRenderer.SetEnabled(true);
            if (isActiveAndEnabled) Activate();
        }
        else
        {
            Deactivate();
        }
    }

    private RenderTextureDescriptor GetGrabDescriptor()
    {
        var descriptor = GetCameraTargetDescriptor();
        descriptor.depthBufferBits = 0;
        descriptor.depthStencilFormat = GraphicsFormat.None;
        descriptor.msaaSamples = 1;
        descriptor.useDynamicScale = false;
        descriptor.useDynamicScaleExplicit = false;
        return descriptor;
    }

    private RenderTextureDescriptor GetCameraTargetDescriptor()
    {
        if (activeCamera.targetTexture != null)
        {
            var descriptor = activeCamera.targetTexture.descriptor;
            descriptor.width = Mathf.Max(activeCamera.scaledPixelWidth, 1);
            descriptor.height = Mathf.Max(activeCamera.scaledPixelHeight, 1);
            return descriptor;
        }

        return new RenderTextureDescriptor(
            Mathf.Max(activeCamera.scaledPixelWidth, 1),
            Mathf.Max(activeCamera.scaledPixelHeight, 1),
            activeCamera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default,
            0)
        {
            sRGB = QualitySettings.activeColorSpace == ColorSpace.Linear
        };
    }

}
