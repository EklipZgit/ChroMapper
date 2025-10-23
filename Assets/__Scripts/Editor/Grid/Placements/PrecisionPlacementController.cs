using UnityEngine;

public class PrecisionPlacementController : MonoBehaviour
{
    public static bool IsEnabled;

    [SerializeField] private IntersectionCollider intersectionCollider;
    [SerializeField] private Renderer regularMesh;
    [SerializeField] private Renderer expandedMesh;
    [SerializeField] private GridLane lane;

    [SerializeField] private Vector4 precisionSnap = new(1f, 0f, 0f, 0f);

    private MaterialPropertyBlock materialPropertyBlock;
    private static readonly int positionID = Shader.PropertyToID("_MousePosition");
    private static readonly int gridSpacingID = Shader.PropertyToID("_GridSpacing");
    private static readonly int gridOffsetID = Shader.PropertyToID("_GridOffset");

    private void Start()
    {
        materialPropertyBlock = new MaterialPropertyBlock();

        Settings.NotifyBySettingName(nameof(Settings.PrecisionPlacementGridPrecision), UpdatePrecisionGrid);
        GridViewController.OnGridViewUpdated += UpdateGridPosition;

        TogglePrecisionPlacement(false);
        UpdatePrecisionGrid(Settings.Instance.PrecisionPlacementGridPrecision);
    }

    private void OnDestroy()
    {
        Settings.ClearSettingNotifications(nameof(Settings.PrecisionPlacementGridPrecision));
        GridViewController.OnGridViewUpdated -= UpdateGridPosition;
    }

    public void TogglePrecisionPlacement(bool toggle)
    {
        if (toggle == IsEnabled) return;
        IsEnabled = toggle;

        if (toggle && Settings.Instance.PrecisionPlacementMode != PrecisionPlacementMode.Off)
        {
            expandedMesh.gameObject.SetActive(true);
            intersectionCollider.Size = expandedMesh.transform.localScale;
        }
        else
        {
            expandedMesh.gameObject.SetActive(false);
            intersectionCollider.Size = regularMesh.transform.localScale;
        }

        intersectionCollider.HardRefresh();
    }

    public void UpdateMousePosition(Vector3 mousePosition)
    {
        materialPropertyBlock.SetVector(positionID, mousePosition);
        expandedMesh.SetPropertyBlock(materialPropertyBlock);
    }

    private void UpdatePrecisionGrid(object value)
    {
        var snapping = (int)value;
        float gridSeparation = CMMath.GetLowestDenominator(snapping);
        if (gridSeparation < 1) gridSeparation = 1;

        precisionSnap[0] = 1f;

        var useSegments = gridSeparation <= snapping;
        precisionSnap[1] = useSegments ? 1f / gridSeparation : 0f;

        var useDetailedSegments = gridSeparation < snapping;
        gridSeparation *= CMMath.GetLowestDenominator(Mathf.FloorToInt(snapping / gridSeparation));
        precisionSnap[2] = useDetailedSegments ? 1f / gridSeparation : 0f;

        var usePreciseSegments = gridSeparation < snapping;
        gridSeparation *= CMMath.GetLowestDenominator(Mathf.FloorToInt(snapping / gridSeparation));
        precisionSnap[3] = usePreciseSegments ? 1f / gridSeparation : 0f;

        materialPropertyBlock.SetVector(gridSpacingID, precisionSnap);
        expandedMesh.SetPropertyBlock(materialPropertyBlock);
    }

    private void UpdateGridPosition()
    {
        materialPropertyBlock.SetVector(gridOffsetID, (Vector3)lane.XYOffset + lane.LocalOffset);
        expandedMesh.SetPropertyBlock(materialPropertyBlock);
    }
}
