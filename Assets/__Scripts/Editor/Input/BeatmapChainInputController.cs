using Beatmap.Appearances;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BeatmapChainInputController : BeatmapInputController<ChainContainer>, CMInput.IChainObjectsActions
{
    private const int minChainCount = 1;
    private const int maxChainCount = 999;
    private const float minChainSquish = 0.1f;
    private const float maxChainSquish = 999;

    [SerializeField] private ScrollPrecisionController scrollPrecisionController;
    [SerializeField] private ChainAppearanceSO chainAppearance;

    public void OnTweakChainCount(InputAction.CallbackContext context)
    {
        if (!context.performed || !IsHovering || HoveredObject.Dragged) return;

        var modifier = context.GetScrollDirection(Settings.Instance.InvertScrollChainSegmentCount);
        TweakValue(HoveredObject, modifier);
    }

    public void TweakValue(ChainContainer c, int modifier)
    {
        var sliceCount = Mathf.Clamp(c.ChainData.SliceCount + modifier, minChainCount, maxChainCount);
        
        ChainCommand.SetSliceCount(c.ChainData, sliceCount);
    }

    public void OnTweakChainSquish(InputAction.CallbackContext context)
    {
        if (!context.performed || !IsHovering || HoveredObject.Dragged) return;
        
        var modifier = context.GetScrollDirection(Settings.Instance.InvertScrollChainSquish)
            * scrollPrecisionController.GetCurrentMultiplierPrecision();
        TweakChainSquish(HoveredObject, modifier);
    }

    public void TweakChainSquish(ChainContainer c, float modifier)
    {
        var squish = Mathf.Clamp(c.ChainData.Squish + modifier, minChainSquish, maxChainSquish);
        
        ChainCommand.SetSquish(c.ChainData, squish);
    }
}
