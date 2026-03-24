using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class NotePlacement : BasePlacement<BaseNote, NoteContainer, NoteGridContainer>,
                             CMInput.INotePlacementActions
{
    private const int upKey = 0;
    private const int leftKey = 1;
    private const int downKey = 2;
    private const int rightKey = 3;

    // Chroma Color Stuff
    public static readonly string ChromaColorKey = "PlaceChromaObjects";

    private static readonly int alwaysTranslucent = Shader.PropertyToID("_AlwaysTranslucent");
    [SerializeField] private NoteAppearanceSO noteAppearanceSo;
    [SerializeField] private DeleteToolController deleteToolController;
    [SerializeField] private LaserSpeedController laserSpeedController;
    [SerializeField] private BeatmapNoteInputController beatmapNoteInputController;
    [SerializeField] private ColorPicker colorPicker;
    [SerializeField] private ToggleColourDropdown dropdown;

    [SerializeField] private CameraManager cameraManager;

    // TODO: Perhaps move this into Settings as a user-configurable option
    private readonly float
        diagonalStickMaxTime = 0.3f; // This controls the maximum time that a note will stay a diagonal

    // REVIEW: Perhaps partner with Obama to turn this list of bools
    // into some binary shifting goodness
    private readonly List<bool> heldKeys = new() { false, false, false, false };

    private bool diagonal;
    private bool flagDirectionsUpdate;
    private bool updateAttachedSliderDirection;

    // Chroma Color Check
    public static bool CanPlaceChromaObjects
    {
        get
        {
            if (Settings.NonPersistentSettings.ContainsKey(ChromaColorKey))
                return (bool)Settings.NonPersistentSettings[ChromaColorKey];
            return false;
        }
    }

    private void LateUpdate()
    {
        if (flagDirectionsUpdate)
        {
            HandleDirectionValues();
            flagDirectionsUpdate = false;
        }
    }

    //TODO perhaps make a helper function to deal with the context.performed and context.canceled checks
    public void OnDownNote(InputAction.CallbackContext context) => HandleKeyUpdate(context, downKey);

    public void OnLeftNote(InputAction.CallbackContext context) => HandleKeyUpdate(context, leftKey);

    public void OnUpNote(InputAction.CallbackContext context) => HandleKeyUpdate(context, upKey);

    public void OnRightNote(InputAction.CallbackContext context) => HandleKeyUpdate(context, rightKey);

    public void OnDotNote(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        DeleteToolController.UpdateDeletion(false);
        UpdateCut((int)NoteCutDirection.Any);
    }

    public void OnUpLeftNote(InputAction.CallbackContext context)
    {
        if (context.performed && !laserSpeedController.Activated) UpdateCut((int)NoteCutDirection.UpLeft);
    }

    public void OnUpRightNote(InputAction.CallbackContext context)
    {
        if (context.performed && !laserSpeedController.Activated) UpdateCut((int)NoteCutDirection.UpRight);
    }

    public void OnDownRightNote(InputAction.CallbackContext context)
    {
        if (context.performed && !laserSpeedController.Activated) UpdateCut((int)NoteCutDirection.DownRight);
    }

    public void OnDownLeftNote(InputAction.CallbackContext context)
    {
        if (context.performed && !laserSpeedController.Activated) UpdateCut((int)NoteCutDirection.DownLeft);
    }

    // Toggle Chroma Color Function
    public void PlaceChromaObjects(bool v) => Settings.NonPersistentSettings[ChromaColorKey] = v;

    protected override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> container) =>
        new BeatmapObjectPlacementAction(spawned, container, "Placed a note.");

    protected override BaseNote GenerateOriginalData() =>
        new() { Color = (int)NoteColor.Red, CutDirection = (int)NoteCutDirection.Down };

    public override void Initialize(PlacementProvider provider)
    {
        base.Initialize(provider);
        UpdateAppearance();
    }

    protected override void HandleHitToPlacement(Intersections.IntersectionHit hit, Vector3 localPoint)
    {
        var zPlacement = BeatmapPositionHelper.SongTimeToLanePositionZ(SongBpmTime);

        if (PrecisionPlacementController.IsEnabled)
        {
            var precision = Settings.Instance.PrecisionPlacementGridPrecision;
            LanePosition = BeatmapPositionHelper.LocalPositionToLanePositionRound(localPoint, precision, BeatmapConstant.PlayerYOffset / 2f);
            LanePosition.z = zPlacement;
            PlacementVisualContainer.transform.localPosition =
                BeatmapPositionHelper.LanePositionToLocalPosition(LanePosition, BeatmapConstant.PlayerYOffset / 2f);
        }
        else
        {
            LanePosition = BeatmapPositionHelper.LocalPositionToLanePosition(
                localPoint,
                BeatmapConstant.PlayerYOffset / 2f);
            LanePosition.z = zPlacement;
            PlacementVisualContainer.transform.localPosition =
                BeatmapPositionHelper.LanePositionToLocalPosition(
                    LanePosition,
                    Bounds,
                    BeatmapConstant.PlayerYOffset / 2f);
        }
    }

    protected override void HandlePlacementToData(PlacementInputState inputState)
    {
        // Check if Chroma Color notes button is active and apply _color
        QueuedData.CustomColor = CanPlaceChromaObjects && dropdown.Visible
            ? colorPicker.CurrentColor
            : null;

        var pos = LanePosition;
        pos.x += 2f;

        var vanillaX = Mathf.FloorToInt(Mathf.Clamp(pos.x, 0f, 3f));
        var vanillaY = Mathf.FloorToInt(Mathf.Clamp(pos.y, 0f, 2f));

        QueuedData.PosX = vanillaX;
        QueuedData.PosY = vanillaY;

        if (PrecisionPlacementController.IsEnabled)
            QueuedData.CustomCoordinate = new Vector2(pos.x - 2f, pos.y) - (Vector2.one / 2f);
        else
        {
            QueuedData.CustomCoordinate =
                !(Mathf.Approximately(vanillaX, pos.x)
                    && Mathf.Approximately(vanillaY, pos.y))
                    ? new Vector2(pos.x - 2f, pos.y) - (Vector2.one / 2f)
                    : null;
        }
    }

    public NoteContainer ObjectUnderCursor()
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return null;

        var ray = cameraManager.SelectedCameraController.Camera.ScreenPointToRay(Mouse.current.position.ReadValue());
        return !Intersections.Raycast(ray, 9, out var hit)
            ? null
            : hit.GameObject.GetComponentInParent<NoteContainer>();
    }

    public void UpdateCut(int value)
    {
        ToggleDiagonalAngleOffset(QueuedData, value);
        QueuedData.CutDirection = value;
        if (DraggedObjectContainer != null && DraggedObjectContainer.NoteData != null)
        {
            ToggleDiagonalAngleOffset(DraggedObjectContainer.NoteData, value);
            DraggedObjectContainer.NoteData.CutDirection = value;
            noteAppearanceSo.SetNoteAppearance(DraggedObjectContainer);
            updateAttachedSliderDirection = true;
        }
        // TODO: This IsActive is a workaround to prevent ghost notes. This happens because bomb placement could be
        //       dragging a note and quick editing results in issues
        else if (AllowPlacement
            && beatmapNoteInputController.QuickModificationActive
            && Settings.Instance.QuickNoteEditing)
        {
            var note = ObjectUnderCursor();
            if (note != null && note.ObjectData is BaseNote noteData)
            {
                var originalData = BeatmapFactory.Clone(noteData);
                ToggleDiagonalAngleOffset(noteData, value);
                noteData.CutDirection = value;

                var actions = new List<BeatmapAction>
                {
                    new BeatmapObjectModifiedAction(
                        noteData,
                        noteData,
                        originalData,
                        "Quick edit",
                        true,
                        ActionMergeType.NoteDirectionChange)
                };
                CommonNotePlacement.UpdateAttachedSlidersDirection(noteData, actions);

                if (actions.Count > 1)
                {
                    BeatmapActionContainer.AddAction(
                        new ActionCollectionAction(
                            actions,
                            true,
                            false,
                            "Quick edit",
                            ActionMergeType.NoteDirectionChange),
                        true);
                    SelectionController.OnSelectionChanged?.Invoke();
                }
                else
                    BeatmapActionContainer.AddAction(actions[0], true);
            }
        }

        UpdateAppearance();
    }

    private void ToggleDiagonalAngleOffset(BaseNote note, int newCutDirection)
    {
        if (note.CutDirection == (int)NoteCutDirection.Any
            && newCutDirection == (int)NoteCutDirection.Any
            && note.AngleOffset != 45)
            note.AngleOffset = 45;
        else
            note.AngleOffset = 0;
    }

    public void UpdateType(int type)
    {
        QueuedData.Type = type;
        UpdateAppearance();
    }

    private void UpdateAppearance()
    {
        if (PlacementVisualContainer is null) return;
        PlacementVisualContainer.NoteData = QueuedData;
        noteAppearanceSo.SetNoteAppearance(PlacementVisualContainer);
        PlacementVisualContainer.ModelController.MpbController.Mpb.SetFloat(alwaysTranslucent, 1);
        PlacementVisualContainer.UpdateMaterials();
        PlacementVisualContainer.DirectionTarget.localEulerAngles = NoteContainer.Directionalize(QueuedData);
    }

    protected override void TransferQueuedToDraggedObject(ref BaseNote dragged, BaseNote queued)
    {
        dragged.JsonTime = queued.JsonTime;
        dragged.PosX = queued.PosX;
        dragged.PosY = queued.PosY;
        dragged.CutDirection = queued.CutDirection;
        dragged.CustomCoordinate = queued.CustomCoordinate;
        if (DraggedObjectContainer != null)
        {
            DraggedObjectContainer.DirectionTarget.localEulerAngles = NoteContainer.Directionalize(dragged);
            DraggedObjectContainer.DirectionTargetEuler = NoteContainer.Directionalize(dragged);
        }

        noteAppearanceSo.SetNoteAppearance(DraggedObjectContainer);

        TransferQueuedToAttachedDraggedSliders(queued);
    }

    private void TransferQueuedToAttachedDraggedSliders(BaseNote queued)
    {
        var epsilon = BeatmapObjectContainerCollection.Epsilon;
        foreach (var baseSlider in DraggedAttachedSliderDatas[IndicatorType.Head])
        {
            baseSlider.JsonTime = queued.JsonTime;
            baseSlider.PosX = queued.PosX;
            baseSlider.PosY = queued.PosY;
            if (updateAttachedSliderDirection) baseSlider.CutDirection = queued.CutDirection;
            baseSlider.CustomCoordinate = queued.CustomCoordinate;
        }

        foreach (var baseSlider in DraggedAttachedSliderDatas[IndicatorType.Tail])
        {
            baseSlider.TailJsonTime = queued.JsonTime;
            baseSlider.TailPosX = queued.PosX;
            baseSlider.TailPosY = queued.PosY;
            baseSlider.CustomTailCoordinate = queued.CustomCoordinate;

            if (baseSlider is BaseArc baseArc && updateAttachedSliderDirection)
                baseArc.TailCutDirection = queued.CutDirection;
        }

        foreach (var baseSliderContainer in DraggedAttachedSliderContainers)
        {
            switch (baseSliderContainer)
            {
                case ArcContainer arcContainer:
                    arcContainer.NotifySplineChanged();
                    break;
                case ChainContainer chainContainer:
                    chainContainer.AdjustTimePlacement();
                    chainContainer.GenerateChain();
                    break;
            }
        }

        updateAttachedSliderDirection = false;
    }

    public override void CreateVisual()
    {
        base.CreateVisual();
        PlacementVisualContainer.SetArcVisible(false);
    }

    private void HandleKeyUpdate(InputAction.CallbackContext context, int id)
    {
        if (context.performed ^ heldKeys[id]) flagDirectionsUpdate = true;
        heldKeys[id] = context.performed;
    }

    private void HandleDirectionValues()
    {
        DeleteToolController.UpdateDeletion(false);

        var upNote = heldKeys[upKey];
        var downNote = heldKeys[downKey];
        var leftNote = heldKeys[leftKey];
        var rightNote = heldKeys[rightKey];
        var previousDiagonalState = diagonal;

        var handleUpDownNotes = upNote ^ downNote; // XOR: True if the values are different, false if the same
        var handleLeftRightNotes = leftNote ^ rightNote;

        diagonal = handleUpDownNotes && handleLeftRightNotes;

        if (previousDiagonalState && !diagonal)
        {
            StartCoroutine(CheckForDiagonalUpdate());
            return;
        }

        if (handleUpDownNotes && !handleLeftRightNotes) // We handle simple up/down notes
        {
            if (upNote)
                UpdateCut((int)NoteCutDirection.Up);
            else
                UpdateCut((int)NoteCutDirection.Down);
        }
        else if (!handleUpDownNotes && handleLeftRightNotes) // We handle simple left/right notes
        {
            if (leftNote)
                UpdateCut((int)NoteCutDirection.Left);
            else
                UpdateCut((int)NoteCutDirection.Right);
        }
        else if (diagonal) //We need to do a diagonal
        {
            if (leftNote)
            {
                if (upNote)
                    UpdateCut((int)NoteCutDirection.UpLeft);
                else
                    UpdateCut((int)NoteCutDirection.DownLeft);
            }
            else
            {
                if (upNote)
                    UpdateCut((int)NoteCutDirection.UpRight);
                else
                    UpdateCut((int)NoteCutDirection.DownRight);
            }
        }
    }

    private IEnumerator CheckForDiagonalUpdate()
    {
        var previousHeldKeys = new List<bool>(heldKeys);
        yield return new WaitForSeconds(diagonalStickMaxTime);
        // Weird way of saying "Are the keys being held right now the same as before"
        if (!previousHeldKeys
            .Except(heldKeys)
            .Any())
            flagDirectionsUpdate = true;
    }
}
