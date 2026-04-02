using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EventBoxViewController : MonoBehaviour
{
    [SerializeField] private BeatmapRuntimeContext beatmapRuntimeContext;
    [SerializeField] private EditModeContext editModeContext;
    [SerializeField] private ScrollPrecisionController scrollPrecisionController;
    [SerializeField] private GLSEventGridProvider glsEventGridProvider;
    [SerializeField] private GameObject targetObject;

    [Header("Event Box Tool")] [SerializeField]
    private ButtonComponent addEventBoxButton;

    [SerializeField] private ButtonComponent deleteEventBoxButton;

    [Header("ID Tab")] [SerializeField] private ToggleComponent idTabPrefab;
    [SerializeField] private RectTransform idTabTargetTransform;
    private readonly List<ToggleComponent> instantiatedIdTab = new();

    [Header("Info Text")] [SerializeField] private TextMeshProUGUI eventBoxIdText;
    [SerializeField] private TextMeshProUGUI filteredIdText;
    [SerializeField] private Image idImagePrefab;
    [SerializeField] private Transform idImageTargetTransform;
    private readonly List<Image> instantiatedIdImage = new();

    [SerializeField] private TextMeshProUGUI errorTextPrefab;
    [SerializeField] private Transform errorTextTargetTransform;
    private readonly List<TextMeshProUGUI> instantiatedErrorText = new();

    [Header("Input")] [SerializeField] private GameObject inputContainer;
    [Space] [SerializeField] private ToggleComponent beatDistributionWaveToggle;
    [SerializeField] private ToggleComponent beatDistributionStepToggle;
    [SerializeField] private TextBoxFloatComponent beatDistributionInput;
    [Space] [SerializeField] private ToggleComponent filterTypeSectionToggle;
    [SerializeField] private ToggleComponent filterTypeStepToggle;
    [SerializeField] private TextBoxIntComponent chunkInput;
    [SerializeField] private ToggleComponent reverseToggle;
    [SerializeField] private TextBoxIntComponent p0Input;
    [SerializeField] private TextBoxIntComponent p1Input;
    [SerializeField] private ToggleComponent randomToggle;
    [SerializeField] private ToggleComponent inOrderToggle;
    [SerializeField] private TextBoxIntComponent seedInput;
    [SerializeField] private GameObject axisObject;
    [SerializeField] private ToggleComponent axisXToggle;
    [SerializeField] private ToggleComponent axisYToggle;
    [SerializeField] private ToggleComponent axisZToggle;
    [SerializeField] private ToggleComponent flipToggle;
    [Space] [SerializeField] private TextBoxFloatComponent limitInput;
    [SerializeField] private ToggleComponent limitDurationToggle;
    [SerializeField] private ToggleComponent limitDistributionToggle;
    [Space] [SerializeField] private ToggleComponent valueDistributionWaveToggle;
    [SerializeField] private ToggleComponent valueDistributionStepToggle;
    [SerializeField] private TextBoxFloatComponent valueDistributionInput;
    [SerializeField] private ToggleComponent affectFirstToggle;
    [SerializeField] private DropdownComponent easeTypeDropdown;

    private BaseEventBoxGroup groupContext;
    private BaseEventBox boxContext;
    private int boxIndex;

    private void Start()
    {
        editModeContext.OnEditModeChanged += HandleEditModeChanged;
        glsEventGridProvider.OnGroupChanged += HandleGroupChanged;

        addEventBoxButton.OnClick(HandleAddEventBox);
        deleteEventBoxButton.OnClick(HandleDeleteEventBox);

        beatDistributionWaveToggle.OnValueChanged(HandleBeatDistributionWaveValueChanged);
        beatDistributionStepToggle.OnValueChanged(HandleBeatDistributionStepValueChanged);
        beatDistributionInput
            .WithScrollPrecision(scrollPrecisionController.GetCurrentTimePrecision)
            .WithInvertScroll(() => Settings.Instance.InvertScrollTime)
            .OnEndEdit(HandleBeatDistributionValueChanged)
            .OnValueChanged(HandleBeatDistributionValueChanged);
        filterTypeSectionToggle.OnValueChanged(HandleFilterTypeSectionValueChanged);
        filterTypeStepToggle.OnValueChanged(HandleFilterTypeStepValueChanged);
        chunkInput
            .WithInvertScroll(() => Settings.Instance.InvertScrollEventValue)
            .OnEndEdit(HandleChunkValueChanged)
            .OnValueChanged(HandleChunkValueChanged);
        reverseToggle.OnValueChanged(HandleReverseValueChanged);
        p0Input
            .WithInvertScroll(() => Settings.Instance.InvertScrollEventValue)
            .OnEndEdit(HandleParam0ValueChanged)
            .OnValueChanged(HandleParam0ValueChanged);
        p1Input
            .WithInvertScroll(() => Settings.Instance.InvertScrollEventValue)
            .OnEndEdit(HandleParam1ValueChanged)
            .OnValueChanged(HandleParam1ValueChanged);
        randomToggle.OnValueChanged(HandleRandomValueChanged);
        inOrderToggle.OnValueChanged(HandleInOrderValueChanged);
        seedInput
            .WithInvertScroll(() => Settings.Instance.InvertScrollEventValue)
            .OnEndEdit(HandleSeedValueChanged)
            .OnValueChanged(HandleSeedValueChanged);
        axisXToggle.OnValueChanged(HandleAxisXValueChanged);
        axisYToggle.OnValueChanged(HandleAxisYValueChanged);
        axisZToggle.OnValueChanged(HandleAxisZValueChanged);
        flipToggle.OnValueChanged(HandleFlipValueChanged);
        limitInput
            .WithScrollPrecision(scrollPrecisionController.GetCurrentPercentPrecision)
            .WithInvertScroll(() => Settings.Instance.InvertScrollEventValue)
            .OnEndEdit(HandleLimitValueChanged)
            .OnValueChanged(HandleLimitValueChanged);
        limitDurationToggle.OnValueChanged(HandleLimitDurationValueChanged);
        limitDistributionToggle.OnValueChanged(HandleLimitDistributionValueChanged);
        valueDistributionWaveToggle.OnValueChanged(HandleValueDistributionWaveValueChanged);
        valueDistributionStepToggle.OnValueChanged(HandleValueDistributionStepValueChanged);
        valueDistributionInput
            .WithInvertScroll(() => Settings.Instance.InvertScrollEventValue)
            .OnEndEdit(HandleValueDistributionValueChanged)
            .OnValueChanged(HandleValueDistributionValueChanged);
        affectFirstToggle.OnValueChanged(HandleAffectFirstValueChanged);
        easeTypeDropdown.WithOptions(Easing.IDToFullName.Values).OnValueChanged(HandleEaseTypeValueChanged);

        HandleEditModeChanged(editModeContext.EditingMode);
    }

    private void OnDestroy()
    {
        editModeContext.OnEditModeChanged -= HandleEditModeChanged;
        glsEventGridProvider.OnGroupChanged -= HandleGroupChanged;
    }

    private void HandleEditModeChanged(EditingMode mode)
    {
        targetObject.SetActive(mode.HasFlag(EditingMode.EventBox));
        if (!mode.HasFlag(EditingMode.EventBox)) SetBoxIndex(0);
    }

    private void HandleGroupChanged(BaseEventBoxGroup group)
    {
        groupContext = group;
        boxContext = null;
        boxIndex = group.ReadOnlyBoxes.Count > 0 ? Math.Clamp(boxIndex, 0, group.ReadOnlyBoxes.Count - 1) : -1;

        SetBoxIndex(boxIndex);
    }

    private Action<bool> HandleSetBoxIndex(int id)
    {
        return _ =>
        {
            SetBoxIndex(id);
        };
    }

    private void HandleAddEventBox()
    {
        if (groupContext == null) return;
        var targetIndex = boxIndex + 1;
        boxIndex = targetIndex; // pre-emptively set
        GLSEventBoxCommand.AddEventBox(groupContext, targetIndex);
    }

    private void HandleDeleteEventBox()
    {
        if (groupContext == null) return;
        GLSEventBoxCommand.DeleteEventBox(groupContext, boxIndex);
    }

    private void SetBoxIndex(int newIndex)
    {
        if (groupContext == null) return;
        boxIndex = newIndex;
        boxContext = groupContext.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        RefreshID();
        HandleEventBoxChanged(groupContext, boxContext);
    }

    private void RefreshID()
    {
        var count = groupContext.ReadOnlyBoxes.Count;

        int i;
        for (i = 0; i < count; i++)
        {
            ToggleComponent idTab;
            if (i >= instantiatedIdTab.Count)
            {
                idTab = Instantiate(idTabPrefab, idTabTargetTransform);
                idTab.WithLabel((i + 1).ToString());
                idTab.OnValueChanged(HandleSetBoxIndex(i));
                instantiatedIdTab.Add(idTab);
            }
            else
                idTab = instantiatedIdTab[i];

            idTab.SetValueWithoutNotify(i == boxIndex);
            idTab.Selectable.interactable = i != boxIndex;
            idTab.gameObject.SetActive(true);
        }

        for (; i < instantiatedIdTab.Count; i++) instantiatedIdTab[i].gameObject.SetActive(false);

        eventBoxIdText.text = $"1  |  {count}";
    }

    private void HandleEventBoxChanged(BaseEventBoxGroup group, BaseEventBox box)
    {
        var boxes = group.ReadOnlyBoxes;
        var count = group switch
        {
            BaseLightColorEventBoxGroup => beatmapRuntimeContext.Descriptor
                .LightColorGroupEffectManager
                .IdToEffect
                .TryGetValue(group.ID, out var fx)
                ? fx.Count
                : 0,
            BaseLightRotationEventBoxGroup => beatmapRuntimeContext.Descriptor
                .LightRotationGroupEffectManager
                .IdToEffect.TryGetValue(group.ID, out var fx)
                ? fx.Count
                : 0,
            BaseLightTranslationEventBoxGroup => beatmapRuntimeContext.Descriptor
                .LightTranslationGroupEffectManager
                .IdToEffect.TryGetValue(group.ID, out var fx)
                ? fx.Count
                : 0,
            BaseVfxEventEventBoxGroup =>
                beatmapRuntimeContext.Descriptor.FloatFxGroupEffectManager.IdToEffect.TryGetValue(
                    group.ID,
                    out var fx)
                    ? fx.Count
                    : 0,
            _ => 0
        };

        foreach (var t in instantiatedErrorText) Destroy(t);
        instantiatedErrorText.Clear();

        int i;
        for (i = 0; i < count; i++)
        {
            Image idImage;
            if (i >= instantiatedIdImage.Count)
            {
                idImage = Instantiate(idImagePrefab, idImageTargetTransform);
                instantiatedIdImage.Add(idImage);
            }
            else
                idImage = instantiatedIdImage[i];

            idImage.color = new Color(0.1f, 0.1f, 0.1f);
            idImage.gameObject.SetActive(true);
        }

        for (; i < instantiatedIdImage.Count; i++) instantiatedIdImage[i].gameObject.SetActive(false);

        HashSet<int> affectedId = new();
        var currentBoxPassed = false;
        foreach (var (b, x) in boxes.Select((b, x) => (b, x)).Where(b => b.b.GetAxis() == box.GetAxis()))
        {
            var ifh = IndexFilterHelper.Convert(b.IndexFilter, count);
            if (ifh == null)
            {
                if (instantiatedErrorText.Count > 10) continue;
                var t = Instantiate(errorTextPrefab, errorTextTargetTransform);
                t.text = $"[{x + 1}] Filter is invalid";
                t.gameObject.SetActive(true);
                instantiatedErrorText.Add(t);
                continue;
            }

            if (b == box) currentBoxPassed = true;
            foreach (var (element, _, _) in ifh)
            {
                if (0 > element && element >= instantiatedIdTab.Count)
                {
                    if (instantiatedErrorText.Count > 10) continue;
                    var t = Instantiate(errorTextPrefab, errorTextTargetTransform);
                    t.text = $"[{x + 1}] Filter returned OOB ID {element}";
                    t.gameObject.SetActive(true);
                    instantiatedErrorText.Add(t);
                    continue;
                }

                if (affectedId.Add(element))
                {
                    instantiatedIdImage[element].color =
                        b == box ? Color.green : currentBoxPassed ? Color.gray : Color.white;
                }
                else if (b == box) instantiatedIdImage[element].color = Color.red;
            }
        }

        if (box == null)
        {
            inputContainer.SetActive(false);
            return;
        }

        inputContainer.SetActive(true);

        var locIfh = IndexFilterHelper.Convert(box.IndexFilter, count);
        filteredIdText.text = locIfh != null
            ? $"{count}  |  {locIfh.Count}  |  {locIfh.VisibleCount}"
            : $"{count}  |  0  |  0";

        beatDistributionWaveToggle.SetValueWithoutNotify(box.BeatDistributionType == (int)DistributionType.Wave);
        beatDistributionStepToggle.SetValueWithoutNotify(box.BeatDistributionType == (int)DistributionType.Step);
        beatDistributionInput.SetValueWithoutNotify(box.BeatDistribution);

        filterTypeSectionToggle.SetValueWithoutNotify(box.IndexFilter.Type == (int)IndexFilterType.Division);
        filterTypeStepToggle.SetValueWithoutNotify(box.IndexFilter.Type == (int)IndexFilterType.StepAndOffset);
        chunkInput.SetValueWithoutNotify(box.IndexFilter.Chunks);

        reverseToggle.SetValueWithoutNotify(box.IndexFilter.Reverse == 1);
        if (box.IndexFilter.Type == (int)IndexFilterType.Division)
        {
            p0Input.MinValue = 1;
            p0Input.SetValueWithoutNotify(box.IndexFilter.Param0);
            p0Input.SetLabelText("Section");
            p1Input.MinValue = 1;
            p1Input.SetValueWithoutNotify(box.IndexFilter.Param1 + 1);
            p1Input.SetLabelText("ID");
        }
        else
        {
            p0Input.MinValue = 1;
            p0Input.SetValueWithoutNotify(box.IndexFilter.Param0 + 1);
            p0Input.SetLabelText("ID");
            p1Input.MinValue = 0;
            p1Input.SetValueWithoutNotify(box.IndexFilter.Param1);
            p1Input.SetLabelText("Step");
        }

        randomToggle.SetValueWithoutNotify((box.IndexFilter.Random & (int)RandomType.RandomElements) > 0);
        inOrderToggle.SetValueWithoutNotify((box.IndexFilter.Random & (int)RandomType.KeepOrder) > 0);
        seedInput.SetValueWithoutNotify(box.IndexFilter.Seed);

        limitInput.SetValueWithoutNotify(box.IndexFilter.Limit * 100f);
        limitDurationToggle.SetValueWithoutNotify(
            (box.IndexFilter.LimitAffectsType & (int)LimitAlsoAffectType.Duration) > 0);
        limitDistributionToggle.SetValueWithoutNotify(
            (box.IndexFilter.LimitAffectsType & (int)LimitAlsoAffectType.Distribution) > 0);

        easeTypeDropdown.SetValueWithoutNotify(box.Easing);

        var td = beatmapRuntimeContext.TracksDefinition.GetGlsOrDefault(groupContext.ID);
        switch (box)
        {
            case BaseLightColorEventBox lceb:
                axisObject.SetActive(false);
                valueDistributionStepToggle.SetValueWithoutNotify(
                    lceb.BrightnessDistributionType == (int)DistributionType.Step);
                valueDistributionWaveToggle.SetValueWithoutNotify(
                    lceb.BrightnessDistributionType == (int)DistributionType.Wave);
                valueDistributionInput
                    .WithScrollPrecision(scrollPrecisionController.GetCurrentBrightnessPrecision)
                    .SetValueWithoutNotify(
                        lceb.BrightnessDistribution * 100f);
                affectFirstToggle.SetValueWithoutNotify(lceb.BrightnessAffectFirst == 1);
                break;
            case BaseLightRotationEventBox lreb:
                axisObject.SetActive(true);
                axisXToggle.SetValueWithoutNotify(lreb.Axis == (int)Axis.X);
                axisYToggle.SetValueWithoutNotify(lreb.Axis == (int)Axis.Y);
                axisZToggle.SetValueWithoutNotify(lreb.Axis == (int)Axis.Z);
                axisXToggle.Selectable.interactable = td.RotationTracks[0];
                axisYToggle.Selectable.interactable = td.RotationTracks[1];
                axisZToggle.Selectable.interactable = td.RotationTracks[2];
                valueDistributionStepToggle.SetValueWithoutNotify(
                    lreb.RotationDistributionType == (int)DistributionType.Step);
                valueDistributionWaveToggle.SetValueWithoutNotify(
                    lreb.RotationDistributionType == (int)DistributionType.Wave);
                valueDistributionInput
                    .WithScrollPrecision(scrollPrecisionController.GetCurrentRotationPrecision)
                    .SetValueWithoutNotify(
                        lreb.RotationDistribution);
                affectFirstToggle.SetValueWithoutNotify(lreb.RotationAffectFirst == 1);
                break;
            case BaseLightTranslationEventBox lteb:
                axisObject.SetActive(true);
                axisXToggle.SetValueWithoutNotify(lteb.Axis == (int)Axis.X);
                axisYToggle.SetValueWithoutNotify(lteb.Axis == (int)Axis.Y);
                axisZToggle.SetValueWithoutNotify(lteb.Axis == (int)Axis.Z);
                axisXToggle.Selectable.interactable = td.TranslationTracks[0];
                axisYToggle.Selectable.interactable = td.TranslationTracks[1];
                axisZToggle.Selectable.interactable = td.TranslationTracks[2];
                valueDistributionStepToggle.SetValueWithoutNotify(
                    lteb.TranslationDistributionType == (int)DistributionType.Step);
                valueDistributionWaveToggle.SetValueWithoutNotify(
                    lteb.TranslationDistributionType == (int)DistributionType.Wave);
                valueDistributionInput
                    .WithScrollPrecision(scrollPrecisionController.GetCurrentTranslationPrecision)
                    .SetValueWithoutNotify(
                        lteb.TranslationDistribution * 100f);
                affectFirstToggle.SetValueWithoutNotify(lteb.TranslationAffectFirst == 1);
                break;
            case BaseVfxEventEventBox ffeb:
                axisObject.SetActive(false);
                valueDistributionStepToggle.SetValueWithoutNotify(
                    ffeb.VfxDistributionType == (int)DistributionType.Step);
                valueDistributionWaveToggle.SetValueWithoutNotify(
                    ffeb.VfxDistributionType == (int)DistributionType.Wave);
                valueDistributionInput
                    .WithScrollPrecision(scrollPrecisionController.GetCurrentFloatFXPrecision)
                    .SetValueWithoutNotify(
                        ffeb.VfxDistribution * 100f);
                affectFirstToggle.SetValueWithoutNotify(ffeb.VfxAffectFirst == 1);
                break;
            default:
                axisObject.SetActive(false);
                break;
        }
    }

    private void HandleBeatDistributionWaveValueChanged(bool value)
    {
        if (value) GLSEventBoxCommand.SetBeatDistributionType((int)DistributionType.Wave, groupContext, boxIndex);
    }

    private void HandleBeatDistributionStepValueChanged(bool value)
    {
        if (value) GLSEventBoxCommand.SetBeatDistributionType((int)DistributionType.Step, groupContext, boxIndex);
    }

    private void HandleBeatDistributionValueChanged(float value) =>
        GLSEventBoxCommand.SetBeatDistribution(value, groupContext, boxIndex);

    private void HandleFilterTypeSectionValueChanged(bool value)
    {
        if (value) GLSEventBoxCommand.SetType((int)IndexFilterType.Division, groupContext, boxIndex);
    }

    private void HandleFilterTypeStepValueChanged(bool value)
    {
        if (value) GLSEventBoxCommand.SetType((int)IndexFilterType.StepAndOffset, groupContext, boxIndex);
    }

    private void HandleChunkValueChanged(int value) => GLSEventBoxCommand.SetChunk(value, groupContext, boxIndex);

    private void HandleReverseValueChanged(bool value) =>
        GLSEventBoxCommand.SetReverse(value ? 1 : 0, groupContext, boxIndex);

    private void HandleParam0ValueChanged(int value) => GLSEventBoxCommand.SetParam0(value, groupContext, boxIndex);

    private void HandleParam1ValueChanged(int value) => GLSEventBoxCommand.SetParam1(value, groupContext, boxIndex);

    private void HandleRandomValueChanged(bool value) =>
        GLSEventBoxCommand.SetRandom(
            boxContext.IndexFilter.Random ^ (int)RandomType.RandomElements,
            groupContext,
            boxIndex);

    private void HandleInOrderValueChanged(bool value) =>
        GLSEventBoxCommand.SetRandom(boxContext.IndexFilter.Random ^ (int)RandomType.KeepOrder, groupContext, boxIndex);

    private void HandleSeedValueChanged(int value) => GLSEventBoxCommand.SetSeed(value, groupContext, boxIndex);

    private void HandleAxisXValueChanged(bool value)
    {
        if (value) GLSEventBoxCommand.SetAxis((int)Axis.X, groupContext, boxIndex);
    }

    private void HandleAxisYValueChanged(bool value)
    {
        if (value) GLSEventBoxCommand.SetAxis((int)Axis.Y, groupContext, boxIndex);
    }

    private void HandleAxisZValueChanged(bool value)
    {
        if (value) GLSEventBoxCommand.SetAxis((int)Axis.Z, groupContext, boxIndex);
    }

    private void HandleFlipValueChanged(bool value) =>
        GLSEventBoxCommand.SetFlip(value ? 1 : 0, groupContext, boxIndex);

    private void HandleLimitValueChanged(float value) =>
        GLSEventBoxCommand.SetLimit(value / 100f, groupContext, boxIndex);

    private void HandleLimitDurationValueChanged(bool value) =>
        GLSEventBoxCommand.SetLimitAffectsType(
            boxContext.IndexFilter.LimitAffectsType ^ (int)LimitAlsoAffectType.Duration,
            groupContext,
            boxIndex);

    private void HandleLimitDistributionValueChanged(bool value) =>
        GLSEventBoxCommand.SetLimitAffectsType(
            boxContext.IndexFilter.LimitAffectsType ^ (int)LimitAlsoAffectType.Distribution,
            groupContext,
            boxIndex);

    private void HandleValueDistributionWaveValueChanged(bool value)
    {
        if (value) GLSEventBoxCommand.SetValueDistributionType((int)DistributionType.Wave, groupContext, boxIndex);
    }

    private void HandleValueDistributionStepValueChanged(bool value)
    {
        if (value) GLSEventBoxCommand.SetValueDistributionType((int)DistributionType.Step, groupContext, boxIndex);
    }

    private void HandleValueDistributionValueChanged(float value) =>
        GLSEventBoxCommand.SetValueDistribution(value, groupContext, boxIndex);

    private void HandleAffectFirstValueChanged(bool value) =>
        GLSEventBoxCommand.SetAffectFirst(value ? 1 : 0, groupContext, boxIndex);

    private void HandleEaseTypeValueChanged(int value) => GLSEventBoxCommand.SetEasing(value, groupContext, boxIndex);
}
