using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;

public class NJSEventPlacement : BasePlacement<BaseNJSEvent, NJSEventContainer, NJSEventGridContainer>
{
    [SerializeField] private GridLane gridLane;

    // Probably move to easings class at some point
    private readonly List<(int id, string name)> supportedEasings = new()
    {
        ((int)EaseType.None, "None"),
        ((int)EaseType.Linear, "Linear"),
        ((int)EaseType.InQuadratic, "Quadratic In"), // im debating whether EaseIn or InEase is better convention
        ((int)EaseType.OutQuadratic, "Quadratic Out"),
        ((int)EaseType.InOutQuadratic, "Quadratic In Out"),
        ((int)EaseType.InCircular, "Circular In"),
        ((int)EaseType.OutCircular, "Circular Out"),
        ((int)EaseType.InOutCircular, "Circular In Out")
    };

    public override void Start()
    {
        // v2 info cannot switch to v4 version => cannot place and save NJS events
        gameObject.SetActive(BeatSaberSongContainer.Instance.Info.MajorVersion != 2);
        if (!gameObject.activeSelf)
        {
            gridLane.Hide = true;
            gridLane.Controller.DeregisterChild(gridLane);
        }
        else
            base.Start();
    }

    protected override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> conflicts) =>
        new BeatmapObjectPlacementAction(spawned, conflicts, $"Placed a NJS Event at time {spawned.JsonTime}");

    protected override BaseNJSEvent GenerateOriginalData() => new();

    protected override void TransferQueuedToDraggedObject(ref BaseNJSEvent dragged, BaseNJSEvent queued) =>
        dragged.JsonTime = queued.JsonTime;

    public override void HandleApply() => CreateAndOpenNJSDialogue(true);

    public void HandleApplyNoDialogue() => base.HandleApply();

    private void AttemptPlaceNJSChange(string njsInput, int easingDropdownValue, bool extend)
    {
        if (string.IsNullOrEmpty(njsInput) || string.IsNullOrWhiteSpace(njsInput)) return;
        if (float.TryParse(njsInput, out var absoluteNJS))
        {
            if (absoluteNJS <= 0)
            {
                CreateAndOpenNJSDialogue(false);
                return;
            }

            var relativeNJS = absoluteNJS - BeatSaberSongContainer.Instance.MapDifficultyInfo.NoteJumpSpeed;

            QueuedData.Easing = supportedEasings[easingDropdownValue].id;
            QueuedData.RelativeNJS = relativeNJS;
            QueuedData.UsePrevious = extend ? 1 : 0;
            base.HandleApply();
        }
        else
            CreateAndOpenNJSDialogue(false);
    }

    private void CreateAndOpenNJSDialogue(bool isInitialPlacement)
    {
        // TODO: Why aren't we caching this dialogue box? Two bugs:
        //    1) The footer buttons can trigger off the same click that opens this dialogue which causes an instant close
        //    2) Immediately reopening the dialogue box after closing it doesn't work

        var createNJSEventDialogueBox = PersistentUI
            .Instance
            .CreateNewDialogBox()
            .WithTitle("Mapper", "njs.dialog");

        if (!isInitialPlacement)
        {
            createNJSEventDialogueBox
                .AddComponent<TextComponent>()
                .WithInitialValue("Mapper", "njs.dialogue.invalidnumber");
        }

        var diffNJS = BeatSaberSongContainer.Instance.MapDifficultyInfo.NoteJumpSpeed;
        var njsTextInput = createNJSEventDialogueBox
            .AddComponent<TextBoxComponent>()
            .WithLabel("Mapper", "njs")
            .WithInitialValue(diffNJS.ToString(CultureInfo.InvariantCulture));

        var easingDropdown = createNJSEventDialogueBox
            .AddComponent<DropdownComponent>()
            .WithLabel("Mapper", "easing")
            .WithOptions(supportedEasings.Select(x => x.name))
            .WithInitialValue(1);
        // This doesn't seem to change the initial option even though the value has changed
        // so we'll change it anyway on opening the dialogue

        var extendToggle = createNJSEventDialogueBox
            .AddComponent<ToggleComponent>()
            .WithLabel("Mapper", "njs.dialogue.useprevious")
            .WithInitialValue(false);

        createNJSEventDialogueBox.OnQuickSubmit(() => AttemptPlaceNJSChange(
            njsTextInput.Value,
            easingDropdown.Value,
            extendToggle.Value));

        createNJSEventDialogueBox.AddFooterButton(null, "PersistentUI", "cancel");
        createNJSEventDialogueBox.AddFooterButton(
            () => AttemptPlaceNJSChange(njsTextInput.Value, easingDropdown.Value, extendToggle.Value),
            "PersistentUI",
            "ok");

        createNJSEventDialogueBox.Open();

        easingDropdown.Value = 1;
    }
}
