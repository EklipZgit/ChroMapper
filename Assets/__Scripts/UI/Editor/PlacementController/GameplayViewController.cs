using UnityEngine;

public class GameplayViewController : MonoBehaviour
{
    [SerializeField] private ScrollPrecisionController scrollPrecisionController;
    [SerializeField] private ArcPlacement arcPlacement;
    [SerializeField] private ChainPlacement chainPlacement;
    [SerializeField] private PlacementLaneController laneController;

    [SerializeField] private TextBoxFloatComponent arcHeadMultiplierInput;
    [SerializeField] private TextBoxFloatComponent arcTailMultiplierInput;
    [SerializeField] private TextBoxFloatComponent chainSquishInput;
    [SerializeField] private TextBoxIntComponent chainCountInput;

    [SerializeField] private TextBoxIntComponent laneCountInput;
    [SerializeField] private TextBoxIntComponent wallExtendInput;

    private void Start()
    {
        arcHeadMultiplierInput
            .WithScrollPrecision(scrollPrecisionController.GetCurrentMultiplierPrecision)
            .WithInvertScroll(() => Settings.Instance.InvertScrollArcMultiplier)
            .OnEndEdit(HandleArcHeadMultiplierChanged)
            .OnValueChanged(HandleArcHeadMultiplierChanged)
            .SetValueWithoutNotify(Settings.Instance.DefaultArcHeadMultiplier);
        arcTailMultiplierInput
            .WithScrollPrecision(scrollPrecisionController.GetCurrentMultiplierPrecision)
            .WithInvertScroll(() => Settings.Instance.InvertScrollArcMultiplier)
            .OnEndEdit(HandleArcTailMultiplierChanged)
            .OnValueChanged(HandleArcTailMultiplierChanged)
            .SetValueWithoutNotify(Settings.Instance.DefaultArcTailMultiplier);
        chainSquishInput
            .WithScrollPrecision(scrollPrecisionController.GetCurrentMultiplierPrecision)
            .WithInvertScroll(() => Settings.Instance.InvertScrollChainSquish)
            .OnEndEdit(HandleChainSquishChanged)
            .OnValueChanged(HandleChainSquishChanged)
            .SetValueWithoutNotify(Settings.Instance.DefaultChainSquish);
        chainCountInput
            .OnEndEdit(HandleChainCountChanged)
            .OnValueChanged(HandleChainCountChanged)
            .SetValueWithoutNotify(Settings.Instance.DefaultChainSliceCount);

        laneCountInput
            .WithInvertScroll(() => Settings.Instance.InvertScrollChainSegmentCount)
            .OnEndEdit(HandleLaneCountChanged)
            .OnValueChanged(HandleLaneCountChanged)
            .SetValueWithoutNotify(laneController.LaneCount);
        wallExtendInput
            .OnEndEdit(HandleWallExtendChanged)
            .OnValueChanged(HandleWallExtendChanged)
            .SetValueWithoutNotify(laneController.ObstacleLaneExtend);
    }

    private void HandleArcHeadMultiplierChanged(float value) => arcPlacement.HeadMultiplier = value;
    private void HandleArcTailMultiplierChanged(float value) => arcPlacement.TailMultiplier = value;
    private void HandleChainSquishChanged(float value) => chainPlacement.Squish = value;
    private void HandleChainCountChanged(int value) => chainPlacement.SliceCount = value;
    private void HandleLaneCountChanged(int value) => laneController.LaneCount = value;
    private void HandleWallExtendChanged(int value) => laneController.ObstacleLaneExtend = value;
}
