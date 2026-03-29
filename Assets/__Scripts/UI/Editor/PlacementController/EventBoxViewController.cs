using System.Globalization;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using TMPro;
using UnityEngine;

public class EventBoxViewController : MonoBehaviour
{
    [SerializeField] private BeatmapRuntimeContext beatmapRuntimeContext;
    [SerializeField] private EditModeContext editModeContext;
    [SerializeField] private GLSEventGridProvider glsEventGridProvider;
    [SerializeField] private GameObject targetObject;

    [Header("Info Text")] [SerializeField] private TextMeshProUGUI eventBoxIdText;
    [SerializeField] private TextMeshProUGUI filteredIdText;

    [Header("Box")] [SerializeField] private ButtonComponent idPrefab;
    [SerializeField] private RectTransform idTransformTarget;

    [Header("Input")] [SerializeField] private ButtonComponent beatDistributionWaveButton;
    [SerializeField] private ButtonComponent beatDistributionStepButton;
    [SerializeField] private TextBoxComponent beatDistributionInput;
    [Space] [SerializeField] private ButtonComponent filterTypeSectionButton;
    [SerializeField] private ButtonComponent filterTypeStepButton;
    [SerializeField] private TextBoxComponent chunkInput;
    [SerializeField] private ToggleComponent reverseToggle;
    [SerializeField] private TextBoxComponent p0Input;
    [SerializeField] private TextBoxComponent p1Input;
    [SerializeField] private ButtonComponent randomButton;
    [SerializeField] private ButtonComponent inOrderButton;
    [SerializeField] private TextBoxComponent seedInput;
    [SerializeField] private GameObject axisObject;
    [SerializeField] private ButtonComponent axisXButton;
    [SerializeField] private ButtonComponent axisYButton;
    [SerializeField] private ButtonComponent axisZButton;
    [SerializeField] private ToggleComponent flipToggle;
    [Space] [SerializeField] private TextBoxComponent limitInput;
    [SerializeField] private ButtonComponent limitDurationButton;
    [SerializeField] private ButtonComponent limitDistributionButton;
    [Space] [SerializeField] private ButtonComponent valueDistributionWaveButton;
    [SerializeField] private ButtonComponent valueDistributionStepButton;
    [SerializeField] private TextBoxComponent valueDistributionInput;
    [SerializeField] private ToggleComponent affectFirstToggle;
    [SerializeField] private DropdownComponent easeTypeDropdown;

    private void Start()
    {
        editModeContext.OnEditModeChanged += HandleEditModeChanged;
        glsEventGridProvider.OnGroupChanged += HandleGroupChanged;
        HandleEditModeChanged(editModeContext.EditingMode);
        easeTypeDropdown.WithOptions(Easing.IDToFullName.Values);
    }

    private void OnDestroy()
    {
        editModeContext.OnEditModeChanged -= HandleEditModeChanged;
        glsEventGridProvider.OnGroupChanged -= HandleGroupChanged;
    }

    private void HandleEditModeChanged(EditingMode mode) => targetObject.SetActive(mode.HasFlag(EditingMode.EventBox));

    private void HandleGroupChanged(BaseEventBoxGroup group)
    {
        var (box, count) = group switch
        {
            BaseLightColorEventBoxGroup lcebg => ((BaseEventBox)lcebg.Boxes.FirstOrDefault(), lcebg.Boxes.Count),
            BaseLightRotationEventBoxGroup lrebg => (lrebg.Boxes.FirstOrDefault(), lrebg.Boxes.Count),
            BaseLightTranslationEventBoxGroup ltebg => (ltebg.Boxes.FirstOrDefault(), ltebg.Boxes.Count),
            BaseVfxEventEventBoxGroup ffebg => (ffebg.Boxes.FirstOrDefault(), ffebg.Boxes.Count),
            _ => (null, 0)
        };

        foreach (Transform t in idTransformTarget) Destroy(t.gameObject);
        for (var i = 0; i < count; i++)
        {
            var idButton = Instantiate(idPrefab, idTransformTarget);
            idButton.WithLabel((i + 1).ToString());
            idButton.gameObject.SetActive(true);
        }

        eventBoxIdText.text = "1  |  " + count;
        HandleEventBoxChanged(group, box);
    }

    private void HandleEventBoxChanged(BaseEventBoxGroup group, BaseEventBox box)
    {
        var trackDefinition = beatmapRuntimeContext.TracksDefinition.GetGlsOrDefault(group.ID);

        var count = box switch
        {
            BaseLightColorEventBox => beatmapRuntimeContext.Descriptor.LightColorGroupEffectManager.IdToEffect[group.ID]
                .Count,
            BaseLightRotationEventBox => beatmapRuntimeContext.Descriptor.LightColorGroupEffectManager
                .IdToEffect[group.ID].Count,
            BaseLightTranslationEventBox => beatmapRuntimeContext.Descriptor.LightColorGroupEffectManager
                .IdToEffect[group.ID].Count,
            BaseVfxEventEventBox => beatmapRuntimeContext.Descriptor.LightColorGroupEffectManager.IdToEffect[group.ID]
                .Count,
            _ => 0
        };
        var ifh = IndexFilterHelper.Convert(box.IndexFilter, count);
        filteredIdText.text =
            $"{count}  |  {ifh.Count}  |  {ifh.VisibleCount}";
        if (box == null) return;

        // beatDistributionStepButton.SetLabelEnabled(box.BeatDistributionType == (int)DistributionType.Step);
        // beatDistributionWaveButton.SetLabelEnabled(box.BeatDistributionType == (int)DistributionType.Wave);
        beatDistributionInput.SetValueWithoutNotify(box.BeatDistribution.ToString(CultureInfo.InvariantCulture));

        chunkInput.SetValueWithoutNotify(box.IndexFilter.Chunks.ToString(CultureInfo.InvariantCulture));

        reverseToggle.SetValueWithoutNotify(box.IndexFilter.Reverse == 1);
        if (box.IndexFilter.Type == (int)IndexFilterType.Division)
        {
            p0Input.SetValueWithoutNotify(box.IndexFilter.Param0.ToString(CultureInfo.InvariantCulture));
            p0Input.SetLabelText("Section");
            p1Input.SetValueWithoutNotify((box.IndexFilter.Param1 + 1).ToString(CultureInfo.InvariantCulture));
            p1Input.SetLabelText("ID");
        }
        else
        {
            p0Input.SetValueWithoutNotify((box.IndexFilter.Param0 + 1).ToString(CultureInfo.InvariantCulture));
            p0Input.SetLabelText("ID");
            p1Input.SetValueWithoutNotify(box.IndexFilter.Param1.ToString(CultureInfo.InvariantCulture));
            p1Input.SetLabelText("Step");
        }

        seedInput.SetValueWithoutNotify(box.IndexFilter.Seed.ToString(CultureInfo.InvariantCulture));

        limitInput.SetValueWithoutNotify((box.IndexFilter.Limit * 100f).ToString(CultureInfo.InvariantCulture));

        switch (box)
        {
            case BaseLightColorEventBox lceb:
                axisObject.SetActive(false);
                valueDistributionInput.SetValueWithoutNotify(
                    (lceb.BrightnessDistribution * 100f).ToString(CultureInfo.InvariantCulture));
                affectFirstToggle.SetValueWithoutNotify(lceb.BrightnessAffectFirst == 1);
                break;
            case BaseLightRotationEventBox lreb:
                axisObject.SetActive(true);
                valueDistributionInput.SetValueWithoutNotify(
                    lreb.RotationDistribution.ToString(CultureInfo.InvariantCulture));
                affectFirstToggle.SetValueWithoutNotify(lreb.RotationAffectFirst == 1);
                break;
            case BaseLightTranslationEventBox lteb:
                axisObject.SetActive(true);
                valueDistributionInput.SetValueWithoutNotify(
                    (lteb.TranslationDistribution * 100f).ToString(CultureInfo.InvariantCulture));
                affectFirstToggle.SetValueWithoutNotify(lteb.TranslationAffectFirst == 1);
                break;
            case BaseVfxEventEventBox ffeb:
                axisObject.SetActive(false);
                valueDistributionInput.SetValueWithoutNotify(
                    (ffeb.VfxDistribution * 100f).ToString(CultureInfo.InvariantCulture));
                affectFirstToggle.SetValueWithoutNotify(ffeb.VfxAffectFirst == 1);
                break;
            default:
                axisObject.SetActive(false);
                break;
        }
    }
}
