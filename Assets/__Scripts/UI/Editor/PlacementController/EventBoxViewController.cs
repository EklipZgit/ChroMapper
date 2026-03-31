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

        var newGroup = BeatmapFactory.Clone(groupContext);
        var newIndex = boxIndex + 1;
        switch (newGroup)
        {
            case BaseLightColorEventBoxGroup lcebg:
                lcebg.Boxes.Insert(newIndex, new BaseLightColorEventBox());
                break;
            case BaseLightRotationEventBoxGroup lrebg:
                lrebg.Boxes.Insert(newIndex, new BaseLightRotationEventBox());
                break;
            case BaseLightTranslationEventBoxGroup ltebg:
                ltebg.Boxes.Insert(newIndex, new BaseLightTranslationEventBox());
                break;
            case BaseVfxEventEventBoxGroup ffebg:
                ffebg.Boxes.Insert(newIndex, new BaseVfxEventEventBox());
                break;
        }

        TriggerAction(groupContext, newGroup);
        SetBoxIndex(newIndex);
    }

    private void HandleDeleteEventBox()
    {
        if (groupContext == null) return;

        var newGroup = BeatmapFactory.Clone(groupContext);
        var newIndex = boxIndex - 1;
        switch (newGroup)
        {
            case BaseLightColorEventBoxGroup lcebg:
                lcebg.Boxes.RemoveAt(boxIndex);
                break;
            case BaseLightRotationEventBoxGroup lrebg:
                lrebg.Boxes.RemoveAt(boxIndex);
                break;
            case BaseLightTranslationEventBoxGroup ltebg:
                ltebg.Boxes.RemoveAt(boxIndex);
                break;
            case BaseVfxEventEventBoxGroup ffebg:
                ffebg.Boxes.RemoveAt(boxIndex);
                break;
        }

        TriggerAction(groupContext, newGroup);
        SetBoxIndex(newIndex);
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
                    instantiatedIdImage[element].color =
                        b == box ? Color.green : currentBoxPassed ? Color.gray : Color.white;
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
        if (value) SetBeatDistributionType((int)DistributionType.Wave);
    }

    private void HandleBeatDistributionStepValueChanged(bool value)
    {
        if (value) SetBeatDistributionType((int)DistributionType.Step);
    }

    private void HandleBeatDistributionValueChanged(float value)
    {
        SetBeatDistribution(value);
    }

    private void HandleFilterTypeSectionValueChanged(bool value)
    {
        if (value) SetType((int)IndexFilterType.Division);
    }

    private void HandleFilterTypeStepValueChanged(bool value)
    {
        if (value) SetType((int)IndexFilterType.StepAndOffset);
    }

    private void HandleChunkValueChanged(int value)
    {
        SetChunk(value);
    }

    private void HandleReverseValueChanged(bool value)
    {
        SetReverse(value ? 1 : 0);
    }

    private void HandleParam0ValueChanged(int value)
    {
        SetParam0(value);
    }

    private void HandleParam1ValueChanged(int value)
    {
        SetParam1(value);
    }

    private void HandleRandomValueChanged(bool value)
    {
        SetRandom(boxContext.IndexFilter.Random ^ (int)RandomType.RandomElements);
    }

    private void HandleInOrderValueChanged(bool value)
    {
        SetRandom(boxContext.IndexFilter.Random ^ (int)RandomType.KeepOrder);
    }

    private void HandleSeedValueChanged(int value)
    {
        SetSeed(value);
    }

    private void HandleAxisXValueChanged(bool value)
    {
        if (value) SetAxis((int)Axis.X);
    }

    private void HandleAxisYValueChanged(bool value)
    {
        if (value) SetAxis((int)Axis.Y);
    }

    private void HandleAxisZValueChanged(bool value)
    {
        if (value) SetAxis((int)Axis.Z);
    }

    private void HandleFlipValueChanged(bool value)
    {
        SetFlip(value ? 1 : 0);
    }

    private void HandleLimitValueChanged(float value)
    {
        SetLimit(value / 100f);
    }

    private void HandleLimitDurationValueChanged(bool value)
    {
        SetLimitAffectsType(boxContext.IndexFilter.LimitAffectsType ^ (int)LimitAlsoAffectType.Duration);
    }

    private void HandleLimitDistributionValueChanged(bool value)
    {
        SetLimitAffectsType(boxContext.IndexFilter.LimitAffectsType ^ (int)LimitAlsoAffectType.Distribution);
    }

    private void HandleValueDistributionWaveValueChanged(bool value)
    {
        if (value) SetValueDistributionType((int)DistributionType.Wave);
    }

    private void HandleValueDistributionStepValueChanged(bool value)
    {
        if (value) SetValueDistributionType((int)DistributionType.Step);
    }

    private void HandleValueDistributionValueChanged(float value)
    {
        SetValueDistribution(value);
    }

    private void HandleAffectFirstValueChanged(bool value)
    {
        SetAffectFirst(value ? 1 : 0);
    }

    private void HandleEaseTypeValueChanged(int value)
    {
        SetEasing(value);
    }

    public void SetType(int value)
    {
        var newGroup = BeatmapFactory.Clone(groupContext);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null) return;
        newBox.IndexFilter.Type = value;
        TriggerAction(groupContext, newGroup);
    }

    public void SetParam0(int value)
    {
        var newGroup = BeatmapFactory.Clone(groupContext);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null) return;

        newBox.IndexFilter.Param0 = newBox.IndexFilter.Type == (int)IndexFilterType.Division ? value : value - 1;

        TriggerAction(groupContext, newGroup);
    }

    public void SetParam1(int value)
    {
        var newGroup = BeatmapFactory.Clone(groupContext);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null) return;
        newBox.IndexFilter.Param1 = newBox.IndexFilter.Type == (int)IndexFilterType.Division ? value - 1 : value;

        TriggerAction(groupContext, newGroup);
    }

    public void SetReverse(int value)
    {
        var newGroup = BeatmapFactory.Clone(groupContext);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null) return;
        newBox.IndexFilter.Reverse = value;
        TriggerAction(groupContext, newGroup);
    }

    public void SetChunk(int value)
    {
        var newGroup = BeatmapFactory.Clone(groupContext);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null) return;
        newBox.IndexFilter.Chunks = value;
        TriggerAction(groupContext, newGroup);
    }

    public void SetRandom(int value)
    {
        var newGroup = BeatmapFactory.Clone(groupContext);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null) return;
        newBox.IndexFilter.Random = value;
        TriggerAction(groupContext, newGroup);
    }

    public void SetSeed(int value)
    {
        var newGroup = BeatmapFactory.Clone(groupContext);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null) return;
        newBox.IndexFilter.Seed = value;
        TriggerAction(groupContext, newGroup);
    }

    public void SetLimit(float value)
    {
        var newGroup = BeatmapFactory.Clone(groupContext);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null) return;
        newBox.IndexFilter.Limit = value;
        TriggerAction(groupContext, newGroup);
    }

    public void SetLimitAffectsType(int value)
    {
        var newGroup = BeatmapFactory.Clone(groupContext);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null) return;
        newBox.IndexFilter.LimitAffectsType = value;
        TriggerAction(groupContext, newGroup);
    }

    public void SetBeatDistributionType(int value)
    {
        var newGroup = BeatmapFactory.Clone(groupContext);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null) return;
        newBox.BeatDistributionType = value;
        TriggerAction(groupContext, newGroup);
    }

    public void SetBeatDistribution(float value)
    {
        var newGroup = BeatmapFactory.Clone(groupContext);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null) return;
        newBox.BeatDistribution = value;
        TriggerAction(groupContext, newGroup);
    }

    public void SetAxis(int value)
    {
        var newGroup = BeatmapFactory.Clone(groupContext);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null) return;
        switch (newBox)
        {
            case BaseLightRotationEventBox lreb:
                lreb.Axis = value;
                TriggerAction(groupContext, newGroup);
                break;
            case BaseLightTranslationEventBox lteb:
                lteb.Axis = value;
                TriggerAction(groupContext, newGroup);
                break;
        }
    }

    public void SetFlip(int value)
    {
        var newGroup = BeatmapFactory.Clone(groupContext);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null) return;
        switch (newBox)
        {
            case BaseLightRotationEventBox lreb:
                lreb.Flip = value;
                TriggerAction(groupContext, newGroup);
                break;
            case BaseLightTranslationEventBox lteb:
                lteb.Flip = value;
                TriggerAction(groupContext, newGroup);
                break;
        }
    }

    public void SetValueDistribution(float value)
    {
        var newGroup = BeatmapFactory.Clone(groupContext);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null) return;
        switch (newBox)
        {
            case BaseLightColorEventBox lceb:
                lceb.BrightnessDistribution = value / 100f;
                break;
            case BaseLightRotationEventBox lreb:
                lreb.RotationDistribution = value;
                break;
            case BaseLightTranslationEventBox lteb:
                lteb.TranslationDistribution = value / 100f;
                break;
            case BaseVfxEventEventBox ffeb:
                ffeb.VfxDistribution = value / 100f;
                break;
        }

        TriggerAction(groupContext, newGroup);
    }

    public void SetValueDistributionType(int value)
    {
        var newGroup = BeatmapFactory.Clone(groupContext);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null) return;
        switch (newBox)
        {
            case BaseLightColorEventBox lceb:
                lceb.BrightnessDistributionType = value;
                break;
            case BaseLightRotationEventBox lreb:
                lreb.RotationDistributionType = value;
                break;
            case BaseLightTranslationEventBox lteb:
                lteb.TranslationDistributionType = value;
                break;
            case BaseVfxEventEventBox ffeb:
                ffeb.VfxDistributionType = value;
                break;
        }

        TriggerAction(groupContext, newGroup);
    }

    public void SetAffectFirst(int value)
    {
        var newGroup = BeatmapFactory.Clone(groupContext);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null) return;
        switch (newBox)
        {
            case BaseLightColorEventBox lceb:
                lceb.BrightnessAffectFirst = value;
                break;
            case BaseLightRotationEventBox lreb:
                lreb.RotationAffectFirst = value;
                break;
            case BaseLightTranslationEventBox lteb:
                lteb.TranslationAffectFirst = value;
                break;
            case BaseVfxEventEventBox ffeb:
                ffeb.VfxAffectFirst = value;
                break;
        }

        TriggerAction(groupContext, newGroup);
    }

    public void SetEasing(int value)
    {
        var newGroup = BeatmapFactory.Clone(groupContext);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null) return;
        newBox.Easing = value;

        TriggerAction(groupContext, newGroup);
    }

    private static void TriggerAction(BaseEventBoxGroup oldGroup, BaseEventBoxGroup newGroup)
    {
        var action = new BeatmapObjectPlacementAction(newGroup, new[] { oldGroup }, "Modified event box group.");
        action.Redo();
        BeatmapActionContainer.AddAction(action);
    }
}
