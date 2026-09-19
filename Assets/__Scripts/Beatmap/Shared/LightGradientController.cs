using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Shared;
using UnityEngine;
using System.Collections.Generic;
using Beatmap.Containers;

public class LightGradientController : MonoBehaviour
{
    private static readonly int colorA = Shader.PropertyToID("_ColorA");
    private static readonly int colorB = Shader.PropertyToID("_ColorB");
    private static readonly int easingId = Shader.PropertyToID("_EasingID");
    private static readonly int strobeColorA = Shader.PropertyToID("_StrobeColorA");
    private static readonly int strobeColorB = Shader.PropertyToID("_StrobeColorB");
    private static readonly int strobeDurationId = Shader.PropertyToID("_StrobeDuration");
    private static readonly int strobeFadeId = Shader.PropertyToID("_StrobeFade");
    private static readonly int strobeFrequencyAId = Shader.PropertyToID("_StrobeFrequencyA");
    private static readonly int strobeFrequencyBId = Shader.PropertyToID("_StrobeFrequencyB");
    private static readonly int useStrobeColorsId = Shader.PropertyToID("_UseStrobeColors");
    private static readonly int useHsvId = Shader.PropertyToID("_UseHSV");

    [SerializeField] private MeshRenderer meshRenderer;

    private MaterialPropertyBlock materialPropertyBlock;
    private GLSColorTransitionPreview colorTransitionPreview;
    private float ribbonLength;
    private ObjectContainer interactionOwner;
    private IntersectionCollider interactionCollider;

    public bool IsInteractiveTransitionRibbon => interactionCollider != null;
    public bool IsIncomingColorTransition { get; private set; }
    public bool AggregatesSameTimeBoxes { get; private set; }
    public float ColorTimelineStart { get; private set; }
    public float ColorTimelineDuration { get; private set; }
    private Vector3 hitUvX;
    private Vector3 hitUvY;
    private Vector2 hitUvOffset;

    public Vector2 GetHitUv(Vector3 worldPoint)
    {
        var point = meshRenderer.transform.InverseTransformPoint(worldPoint);
        return new Vector2(Vector3.Dot(point, hitUvX), Vector3.Dot(point, hitUvY)) + hitUvOffset;
    }

    public void UpdateColorTimeline(
        GLSColorTimeline timeline, BaseLightColorBase owner, bool incoming,
        EventAppearanceSO appearance, System.Func<float, bool> isBoostAt, bool aggregateSameTimeBoxes = false)
    {
        materialPropertyBlock ??= new MaterialPropertyBlock();
        colorTransitionPreview ??= new GLSColorTransitionPreview();
        IsIncomingColorTransition = incoming;
        AggregatesSameTimeBoxes = aggregateSameTimeBoxes;
        var visible = colorTransitionPreview.UpdateTimeline(
            timeline, owner, incoming, appearance, isBoostAt, materialPropertyBlock, out var start, out var end,
            aggregateSameTimeBoxes);
        ColorTimelineStart = start;
        ColorTimelineDuration = visible ? end - start : 0f;
        SetVisible(visible);
        if (!visible)
            return;
        meshRenderer.SetPropertyBlock(materialPropertyBlock);
        UpdateDuration(end - start);
        var position = transform.localPosition;
        position.z = (start - owner.SongBpmTime) * EditorScaleController.EditorScale * (4f / 3f);
        transform.localPosition = position;
    }

    public void UpdateGradientData(
        ChromaLightGradient gradient,
        BasicEventColorLerpType colorLerpType = BasicEventColorLerpType.RGB,
        ChromaLightGradient strobeGradient = null,
        int? easeType = null,
        float strobeFrequencyA = 0f,
        float strobeFrequencyB = 0f,
        bool strobeFade = false)
    {
        materialPropertyBlock ??= new MaterialPropertyBlock();

        ColorTimelineDuration = 0f;
        materialPropertyBlock.SetVector(colorA, gradient.StartColor);
        materialPropertyBlock.SetVector(colorB, gradient.EndColor);
        materialPropertyBlock.SetInt(
            easingId,
            easeType.HasValue
                ? Easing.EasingShaderId(easeType.Value)
                : Easing.EasingShaderId(gradient.EasingType));
        var renderedStrobeGradient = strobeGradient ?? gradient;
        materialPropertyBlock.SetVector(strobeColorA, renderedStrobeGradient.StartColor);
        materialPropertyBlock.SetVector(strobeColorB, renderedStrobeGradient.EndColor);
        materialPropertyBlock.SetFloat(useStrobeColorsId, strobeGradient != null ? 1f : 0f);
        materialPropertyBlock.SetFloat(strobeDurationId, renderedStrobeGradient.Duration);
        materialPropertyBlock.SetFloat(strobeFadeId, strobeGradient != null && strobeFade ? 1f : 0f);
        materialPropertyBlock.SetFloat(strobeFrequencyAId, strobeFrequencyA);
        materialPropertyBlock.SetFloat(strobeFrequencyBId, strobeFrequencyB);
        // The shader uses the shared enum values to distinguish legacy scalar HSV from true angular HSV.
        materialPropertyBlock.SetInt(useHsvId, (int)colorLerpType);
        
        meshRenderer.SetPropertyBlock(materialPropertyBlock);
    }

    public void UpdateColorTransitionDistribution(
        BaseLightColorBase source,
        BaseLightColorBase transition,
        int lightCount,
        bool sourceBoost,
        bool transitionBoost,
        EventAppearanceSO eventAppearance)
    {
        materialPropertyBlock ??= new MaterialPropertyBlock();
        colorTransitionPreview ??= new GLSColorTransitionPreview();
        colorTransitionPreview.Update(
            source,
            transition,
            lightCount,
            sourceBoost,
            transitionBoost,
            eventAppearance,
            materialPropertyBlock);
    }

    private void OnDestroy() => colorTransitionPreview?.Dispose();

    // note: 4/3rds magic number comes from the fact that events are 0.75m in size
    public void UpdateDuration(float duration)
    {
        ribbonLength = duration * EditorScaleController.EditorScale * (4f / 3);
        transform.localPosition = new Vector3(
            0,
            -0.5f + 0.005f,
            0);
        transform.localScale = new Vector3(ribbonLength, 1, 1);
        // Keep the ribbon collider in the source event's current intersection chunk after event moves.
        SyncInteractionColliderGroup();
    }

    public void SetVisible(bool visible)
    {
        // Ribbon prefab children start inactive, so enabling only their renderer cannot make a GLS transition visible.
        if (gameObject.activeSelf != visible)
            gameObject.SetActive(visible);
        meshRenderer.enabled = visible;
        // Create the ribbon collider lazily so hidden ribbons add no intersection work.
        if (visible)
            EnsureInteractionCollider();
    }

    /// <summary>
    /// Ensures the ribbon has an interaction collider for Basic Event and GLS ribbons.
    /// Creates the collider lazily so hidden ribbons add no intersection work.
    /// </summary>
    private void EnsureInteractionCollider()
    {
        if (interactionCollider != null)
        {
            SyncInteractionColliderGroup();
            return;
        }

        interactionOwner = GetComponentInParent<ObjectContainer>();
        if (interactionOwner == null)
            return;

        var meshFilter = meshRenderer.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
            return;

        // Cache UV projection during collider creation; hover callbacks never discover mesh components or allocate vertex arrays.
        if (interactionOwner is GLSEventContainer or GLSGroupContainer)
            InitializeHitUv(meshFilter.sharedMesh);
        // Configure before re-enabling so IntersectionCollider registers once with valid mesh and chunk data.
        interactionCollider = meshRenderer.gameObject.AddComponent<IntersectionCollider>();
        interactionCollider.enabled = false;
        interactionCollider.Mesh = meshFilter.sharedMesh;
        interactionCollider.CollisionGroups = new List<int> { interactionOwner.ChunkID };
        interactionCollider.enabled = true;
    }

    private void InitializeHitUv(Mesh mesh)
    {
        // Caching performance black magic. It works, don't question it or cthulu will come for you
        var vertices = mesh.vertices;
        var uv = mesh.uv;
        var triangles = mesh.triangles;
        var a = triangles[0];
        var b = triangles[1];
        var c = triangles[2];
        var first = vertices[b] - vertices[a];
        var second = vertices[c] - vertices[a];
        var aa = Vector3.Dot(first, first);
        var ab = Vector3.Dot(first, second);
        var bb = Vector3.Dot(second, second);
        var inverse = 1f / ((aa * bb) - (ab * ab));
        var firstUv = uv[b] - uv[a];
        var secondUv = uv[c] - uv[a];
        hitUvX = ((first * ((bb * firstUv.x) - (ab * secondUv.x)))
            + (second * ((aa * secondUv.x) - (ab * firstUv.x)))) * inverse;
        hitUvY = ((first * ((bb * firstUv.y) - (ab * secondUv.y)))
            + (second * ((aa * secondUv.y) - (ab * firstUv.y)))) * inverse;
        hitUvOffset = uv[a] - new Vector2(Vector3.Dot(vertices[a], hitUvX), Vector3.Dot(vertices[a], hitUvY));
    }

    private void SyncInteractionColliderGroup()
    {
        if (interactionCollider == null || interactionOwner == null)
            return;

        var chunkId = interactionOwner.ChunkID;
        if (interactionCollider.CollisionGroups.Count == 1
            && interactionCollider.CollisionGroups[0] == chunkId)
        {
            return;
        }

        // Re-enable through IntersectionCollider's lifecycle so the custom raycaster receives the new chunk.
        interactionCollider.enabled = false;
        interactionCollider.CollisionGroups.Clear();
        interactionCollider.CollisionGroups.Add(chunkId);
        interactionCollider.enabled = true;
    }
}
