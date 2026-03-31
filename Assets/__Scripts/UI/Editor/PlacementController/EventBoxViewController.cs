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
    [SerializeField] private GLSEventGridProvider glsEventGridProvider;
    [SerializeField] private GameObject targetObject;

    [Header("Event Box Tool")] [SerializeField]
    private ButtonComponent addEventBoxButton;

    [SerializeField] private ButtonComponent deleteEventBoxButton;

    [Header("Info Text")] [SerializeField] private TextMeshProUGUI eventBoxIdText;
    [SerializeField] private TextMeshProUGUI filteredIdText;
    [SerializeField] private Image idImagePrefab;
    [SerializeField] private Transform idImageTransformTarget;
    private readonly List<Image> instantiatedIdImage = new();

    [Header("Box")] [SerializeField] private ToggleComponent idPrefab;
    [SerializeField] private RectTransform idTransformTarget;
    private readonly List<ToggleComponent> instantiatedId = new();

    [Header("Input")] [SerializeField] private ToggleComponent beatDistributionWaveToggle;
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
        beatDistributionInput.OnEndEdit(HandleBeatDistributionValueChanged);
        beatDistributionInput.OnValueChanged(HandleBeatDistributionValueChanged);
        filterTypeSectionToggle.OnValueChanged(HandleFilterTypeSectionValueChanged);
        filterTypeStepToggle.OnValueChanged(HandleFilterTypeStepValueChanged);
        chunkInput.OnEndEdit(HandleChunkValueChanged);
        chunkInput.OnValueChanged(HandleChunkValueChanged);
        reverseToggle.OnValueChanged(HandleReverseValueChanged);
        p0Input.OnEndEdit(HandleParam0ValueChanged);
        p0Input.OnValueChanged(HandleParam0ValueChanged);
        p1Input.OnEndEdit(HandleParam1ValueChanged);
        p1Input.OnValueChanged(HandleParam1ValueChanged);
        randomToggle.OnValueChanged(HandleRandomValueChanged);
        inOrderToggle.OnValueChanged(HandleInOrderValueChanged);
        seedInput.OnEndEdit(HandleSeedValueChanged);
        seedInput.OnValueChanged(HandleSeedValueChanged);
        axisXToggle.OnValueChanged(HandleAxisXValueChanged);
        axisYToggle.OnValueChanged(HandleAxisYValueChanged);
        axisZToggle.OnValueChanged(HandleAxisZValueChanged);
        flipToggle.OnValueChanged(HandleFlipValueChanged);
        limitInput.OnEndEdit(HandleLimitValueChanged);
        limitInput.OnValueChanged(HandleLimitValueChanged);
        limitDurationToggle.OnValueChanged(HandleLimitDurationValueChanged);
        limitDistributionToggle.OnValueChanged(HandleLimitDistributionValueChanged);
        valueDistributionWaveToggle.OnValueChanged(HandleValueDistributionWaveValueChanged);
        valueDistributionStepToggle.OnValueChanged(HandleValueDistributionStepValueChanged);
        valueDistributionInput.OnEndEdit(HandleValueDistributionValueChanged);
        valueDistributionInput.OnValueChanged(HandleValueDistributionValueChanged);
        affectFirstToggle.OnValueChanged(HandleAffectFirstValueChanged);
        easeTypeDropdown.OnValueChanged(HandleEaseTypeValueChanged);

        HandleEditModeChanged(editModeContext.EditingMode);
        easeTypeDropdown.WithOptions(Easing.IDToFullName.Values);
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
        boxIndex = group.ReadOnlyBoxes.Count > 0 ? boxIndex : -1;

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
        GLSEventBoxAction.AddEventBox(groupContext, targetIndex);
        SetBoxIndex(targetIndex);
    }

    private void HandleDeleteEventBox()
    {
        if (groupContext == null) return;
        var targetIndex = boxIndex - 1;
        GLSEventBoxAction.DeleteEventBox(groupContext, targetIndex);
        SetBoxIndex(targetIndex);
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
            ToggleComponent idButton;
            if (i >= instantiatedId.Count)
            {
                idButton = Instantiate(idPrefab, idTransformTarget);
                idButton.WithLabel((i + 1).ToString());
                idButton.OnValueChanged(HandleSetBoxIndex(i));
                instantiatedId.Add(idButton);
            }
            else
                idButton = instantiatedId[i];

            idButton.SetValueWithoutNotify(i == boxIndex);
            idButton.Selectable.interactable = i != boxIndex;
            idButton.gameObject.SetActive(true);
        }

        for (; i < instantiatedId.Count; i++) instantiatedId[i].gameObject.SetActive(false);

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

        int i;
        for (i = 0; i < count; i++)
        {
            Image idImage;
            if (i >= instantiatedIdImage.Count)
            {
                idImage = Instantiate(idImagePrefab, idImageTransformTarget);
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
        foreach (var b in boxes.Where(b => b.GetAxis() == box.GetAxis()))
        {
            var localIfh = IndexFilterHelper.Convert(b.IndexFilter, count);
            if (b == box) currentBoxPassed = true;
            foreach (var (element, _, _) in localIfh)
            {
                if (affectedId.Add(element))
                {
                    instantiatedIdImage[element].color =
                        b == box ? Color.green : currentBoxPassed ? Color.gray : Color.white;
                }
                else if (b == box) instantiatedIdImage[element].color = Color.red;
            }
        }

        if (box == null) return;

        var ifh = IndexFilterHelper.Convert(box.IndexFilter, count);
        filteredIdText.text = $"{count}  |  {ifh.Count}  |  {ifh.VisibleCount}";

        beatDistributionWaveToggle.SetValueWithoutNotify(box.BeatDistributionType == (int)DistributionType.Wave);
        beatDistributionStepToggle.SetValueWithoutNotify(box.BeatDistributionType == (int)DistributionType.Step);
        beatDistributionInput.SetValueWithoutNotify(box.BeatDistribution);

        filterTypeSectionToggle.SetValueWithoutNotify(box.IndexFilter.Type == (int)IndexFilterType.Division);
        filterTypeStepToggle.SetValueWithoutNotify(box.IndexFilter.Type == (int)IndexFilterType.StepAndOffset);
        chunkInput.SetValueWithoutNotify(box.IndexFilter.Chunks);

        reverseToggle.SetValueWithoutNotify(box.IndexFilter.Reverse == 1);
        if (box.IndexFilter.Type == (int)IndexFilterType.Division)
        {
            p0Input.SetValueWithoutNotify(box.IndexFilter.Param0);
            p0Input.SetLabelText("Section");
            p1Input.MinValue = 1;
            p1Input.SetValueWithoutNotify(box.IndexFilter.Param1 + 1);
            p1Input.SetLabelText("ID");
        }
        else
        {
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
                valueDistributionInput.SetValueWithoutNotify(
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
                valueDistributionInput.SetValueWithoutNotify(
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
                valueDistributionInput.SetValueWithoutNotify(
                    lteb.TranslationDistribution * 100f);
                affectFirstToggle.SetValueWithoutNotify(lteb.TranslationAffectFirst == 1);
                break;
            case BaseVfxEventEventBox ffeb:
                axisObject.SetActive(false);
                valueDistributionStepToggle.SetValueWithoutNotify(
                    ffeb.VfxDistributionType == (int)DistributionType.Step);
                valueDistributionWaveToggle.SetValueWithoutNotify(
                    ffeb.VfxDistributionType == (int)DistributionType.Wave);
                valueDistributionInput.SetValueWithoutNotify(
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
        if (value) GLSEventBoxAction.SetBeatDistributionType((int)DistributionType.Wave, groupContext, boxIndex);
    }

    private void HandleBeatDistributionStepValueChanged(bool value)
    {
        if (value) GLSEventBoxAction.SetBeatDistributionType((int)DistributionType.Step, groupContext, boxIndex);
    }

    private void HandleBeatDistributionValueChanged(float value) =>
        GLSEventBoxAction.SetBeatDistribution(value, groupContext, boxIndex);

    private void HandleFilterTypeSectionValueChanged(bool value)
    {
        if (value) GLSEventBoxAction.SetType((int)IndexFilterType.Division, groupContext, boxIndex);
    }

    private void HandleFilterTypeStepValueChanged(bool value)
    {
        if (value) GLSEventBoxAction.SetType((int)IndexFilterType.StepAndOffset, groupContext, boxIndex);
    }

    private void HandleChunkValueChanged(int value) => GLSEventBoxAction.SetChunk(value, groupContext, boxIndex);

    private void HandleReverseValueChanged(bool value) =>
        GLSEventBoxAction.SetReverse(value ? 1 : 0, groupContext, boxIndex);

    private void HandleParam0ValueChanged(int value) => GLSEventBoxAction.SetParam0(value, groupContext, boxIndex);

    private void HandleParam1ValueChanged(int value) => GLSEventBoxAction.SetParam1(value, groupContext, boxIndex);

    private void HandleRandomValueChanged(bool value) =>
        GLSEventBoxAction.SetRandom(
            boxContext.IndexFilter.Random ^ (int)RandomType.RandomElements,
            groupContext,
            boxIndex);

    private void HandleInOrderValueChanged(bool value) =>
        GLSEventBoxAction.SetRandom(boxContext.IndexFilter.Random ^ (int)RandomType.KeepOrder, groupContext, boxIndex);

    private void HandleSeedValueChanged(int value) => GLSEventBoxAction.SetSeed(value, groupContext, boxIndex);

    private void HandleAxisXValueChanged(bool value)
    {
        if (value) GLSEventBoxAction.SetAxis((int)Axis.X, groupContext, boxIndex);
    }

    private void HandleAxisYValueChanged(bool value)
    {
        if (value) GLSEventBoxAction.SetAxis((int)Axis.Y, groupContext, boxIndex);
    }

    private void HandleAxisZValueChanged(bool value)
    {
        if (value) GLSEventBoxAction.SetAxis((int)Axis.Z, groupContext, boxIndex);
    }

    private void HandleFlipValueChanged(bool value) => GLSEventBoxAction.SetFlip(value ? 1 : 0, groupContext, boxIndex);

    private void HandleLimitValueChanged(float value) =>
        GLSEventBoxAction.SetLimit(value / 100f, groupContext, boxIndex);

    private void HandleLimitDurationValueChanged(bool value) =>
        GLSEventBoxAction.SetLimitAffectsType(
            boxContext.IndexFilter.LimitAffectsType ^ (int)LimitAlsoAffectType.Duration,
            groupContext,
            boxIndex);

    private void HandleLimitDistributionValueChanged(bool value) =>
        GLSEventBoxAction.SetLimitAffectsType(
            boxContext.IndexFilter.LimitAffectsType ^ (int)LimitAlsoAffectType.Distribution,
            groupContext,
            boxIndex);

    private void HandleValueDistributionWaveValueChanged(bool value)
    {
        if (value) GLSEventBoxAction.SetValueDistributionType((int)DistributionType.Wave, groupContext, boxIndex);
    }

    private void HandleValueDistributionStepValueChanged(bool value)
    {
        if (value) GLSEventBoxAction.SetValueDistributionType((int)DistributionType.Step, groupContext, boxIndex);
    }

    private void HandleValueDistributionValueChanged(float value) =>
        GLSEventBoxAction.SetValueDistribution(value, groupContext, boxIndex);

    private void HandleAffectFirstValueChanged(bool value) =>
        GLSEventBoxAction.SetAffectFirst(value ? 1 : 0, groupContext, boxIndex);

    private void HandleEaseTypeValueChanged(int value) => GLSEventBoxAction.SetEasing(value, groupContext, boxIndex);
}
