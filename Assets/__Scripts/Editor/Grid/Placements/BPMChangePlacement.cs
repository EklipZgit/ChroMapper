using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Containers;
using UnityEngine;

public class BPMChangePlacement : BasePlacement<BaseBpmEvent, BpmEventContainer, BPMChangeGridContainer>
{
    protected override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> conflicting) =>
        new BeatmapObjectPlacementAction(spawned, conflicting, $"Placed a BPM Event at time {spawned.JsonTime}");

    protected override BaseBpmEvent GenerateOriginalData() => new(0, 100);

    protected override void UpdatePlacement(
        Vector3 _,
        Vector3 roundedHit,
        PlacementState state) =>
        PlacementVisualContainer.transform.localPosition =
            new Vector3(0.5f, 0.5f, PlacementVisualContainer.transform.localPosition.z);

    protected override void TransferQueuedToDraggedObject(ref BaseBpmEvent dragged, BaseBpmEvent queued)
    {
        dragged.JsonTime = queued.JsonTime;
        ObjectContainerCollection.RefreshModifiedBeat();
    }

    protected override void HandleDragged() => ObjectContainerCollection.RefreshModifiedBeat();

    public override void HandleApply() => CreateAndOpenBpmDialogue(true);

    private void AttemptPlaceBpmChange(string obj, bool willResetGrid)
    {
        if (string.IsNullOrEmpty(obj) || string.IsNullOrWhiteSpace(obj)) return;
        if (float.TryParse(obj, out var bpm))
        {
            // Prevent users from shooting themselves in the foot 
            if (bpm <= 0)
            {
                CreateAndOpenBpmDialogue(false);
                return;
            }

            if (willResetGrid
                && (Mathf.Abs(QueuedData.JsonTime - Mathf.Round(QueuedData.JsonTime))
                    > BeatmapObjectContainerCollection.Epsilon))
            {
                // e.g. Placing a bpm event at beat 3.5 will create a bpm event at beat 3 and 4.
                //      The bpm on beat 3 will be such that the bpm event on beat 4 lines with where the cursor is.
                var prevBpm = (float)BeatSaberSongContainer.Instance.Map.BpmAtSongBpmTime(SongBpmTime);

                var prevBeat = Mathf.Floor(QueuedData.JsonTime);
                var nextBeat = Mathf.Ceil(QueuedData.JsonTime);

                // Place an offset bpm event on the previous beat to scale the grid so it "resets"
                var offsetBpm = prevBpm / (QueuedData.JsonTime - prevBeat);
                var offsetEvent = new BaseBpmEvent(prevBeat, offsetBpm);
                ObjectContainerCollection.SpawnObject(offsetEvent, out var offsetConflicting);

                // Place the bpm event on the next beat
                var queuedEvent = new BaseBpmEvent(nextBeat, bpm);
                ObjectContainerCollection.SpawnObject(queuedEvent, out var queuedConflicting);

                BeatmapActionContainer.AddAction(
                    new ActionCollectionAction(
                        new List<BeatmapAction>
                        {
                            GenerateAction(offsetEvent, offsetConflicting),
                            GenerateAction(queuedEvent, queuedConflicting)
                        }));
            }
            else
            {
                QueuedData.Bpm = bpm;
                base.HandleApply();
            }
        }
        else
            CreateAndOpenBpmDialogue(false);
    }

    private void CreateAndOpenBpmDialogue(bool isInitialPlacement)
    {
        // TODO: Why aren't we caching this dialogue box? Two bugs:
        //    1) The footer buttons can trigger off the same click that opens this dialogue which causes an instant close
        //    2) Immediately reopening the dialogue box after closing it doesn't work

        var createBpmEventDialogueBox = PersistentUI
            .Instance
            .CreateNewDialogBox()
            .WithTitle("Mapper", "bpm.dialog");

        if (!isInitialPlacement)
        {
            createBpmEventDialogueBox
                .AddComponent<TextComponent>()
                .WithInitialValue("Mapper", "bpm.dialogue.invalidnumber");
        }

        var lastBpm = (float)BeatSaberSongContainer.Instance.Map.BpmAtSongBpmTime(SongBpmTime);

        var bpmTextInput = createBpmEventDialogueBox
            .AddComponent<TextBoxComponent>()
            .WithLabel("Mapper", "bpm.dialogue.beatsperminute")
            .WithInitialValue(lastBpm.ToString());

        var resetBeatToggle = createBpmEventDialogueBox
            .AddComponent<ToggleComponent>()
            .WithLabel("Mapper", "bpm.dialogue.resetbeat")
            .WithInitialValue(false);

        createBpmEventDialogueBox.OnQuickSubmit(() => AttemptPlaceBpmChange(bpmTextInput.Value, resetBeatToggle.Value));

        createBpmEventDialogueBox.AddFooterButton(null, "PersistentUI", "cancel");
        createBpmEventDialogueBox.AddFooterButton(
            () => AttemptPlaceBpmChange(bpmTextInput.Value, resetBeatToggle.Value),
            "PersistentUI",
            "ok");

        createBpmEventDialogueBox.Open();
    }
}
