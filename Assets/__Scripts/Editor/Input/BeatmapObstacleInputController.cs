using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Helper;
using Beatmap.V2;
using Beatmap.V3;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class BeatmapObstacleInputController : BeatmapInputController<ObstacleContainer>,
                                              CMInput.IObstacleObjectsActions
{
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private BPMChangeGridContainer bpmChangeGridContainer;
    [SerializeField] private ObstacleAppearanceSO obstacleAppearanceSo;
    [SerializeField] private BeatmapRuntimeContext beatmapRuntimeContext;

    public void OnChangeWallDuration(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        RaycastFirstObject(out var obs);
        if (obs != null && !obs.Dragged && context.performed)
        {
            var original = BeatmapFactory.Clone(obs.ObjectData);
            var snapping = 1f / atsc.GridMeasureSnapping;
            snapping *= (context.ReadValue<float>() > 0) ^ Settings.Instance.InvertScrollWallDuration
                ? 1
                : -1;

            obs.ObstacleData.Duration += snapping;
            obs.UpdateGridPosition();
            obstacleAppearanceSo.SetObstacleAppearance(obs, beatmapRuntimeContext);
            BeatmapActionContainer.AddAction(
                new BeatmapObjectModifiedAction(
                    obs.ObjectData,
                    obs.ObjectData,
                    original,
                    mergeType: ActionMergeType.WallDurationTweak));
        }
    }

    public void OnChangeWallLowerBound(InputAction.CallbackContext context)
    {
        if (Settings.Instance.MapVersion < 3
            || CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true))
            return;
        RaycastFirstObject(out var obs);
        if (obs != null && !obs.Dragged && context.performed)
        {
            var original = BeatmapFactory.Clone(obs.ObjectData);
            var tweakValue = (context.ReadValue<float>() > 0) ^ Settings.Instance.InvertScrollWallDuration
                ? 1
                : -1;
            var data = obs.ObjectData as BaseObstacle;
            data.PosY = Mathf.Clamp(data.PosY + tweakValue, 0, 2);
            data.Height = Mathf.Min(data.Height, 5 - data.PosY);
            if (data.CompareTo(original) == 0) return;
            obs.UpdateGridPosition();
            obstacleAppearanceSo.SetObstacleAppearance(obs, beatmapRuntimeContext);
            BeatmapActionContainer.AddAction(
                new BeatmapObjectModifiedAction(
                    obs.ObjectData,
                    obs.ObjectData,
                    original,
                    mergeType: ActionMergeType.WallLowerBoundTweak));
        }
    }

    public void OnChangeWallUpperBound(InputAction.CallbackContext context)
    {
        if (Settings.Instance.MapVersion < 3
            || CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true))
            return;
        RaycastFirstObject(out var obs);
        if (obs != null && !obs.Dragged && context.performed)
        {
            var original = BeatmapFactory.Clone(obs.ObjectData);
            var tweakValue = (context.ReadValue<float>() > 0) ^ Settings.Instance.InvertScrollWallDuration
                ? 1
                : -1;
            var data = obs.ObjectData as BaseObstacle;
            data.Height = Mathf.Clamp(data.Height + tweakValue, 1, 5 - data.PosY);
            if (data.CompareTo(original) == 0) return;
            obs.UpdateGridPosition();
            obstacleAppearanceSo.SetObstacleAppearance(obs, beatmapRuntimeContext);
            BeatmapActionContainer.AddAction(
                new BeatmapObjectModifiedAction(
                    obs.ObjectData,
                    obs.ObjectData,
                    original,
                    mergeType: ActionMergeType.WallUpperBoundTweak));
        }
    }

    public void OnToggleHyperWall(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        RaycastFirstObject(out var obs);
        if (obs != null && !obs.Dragged && context.performed) ToggleHyperWall(obs);
    }

    public void ToggleHyperWall(ObstacleContainer obs)
    {
        var wall = BeatmapFactory.Clone(obs.ObjectData) as BaseObstacle;
        wall.JsonTime += obs.ObstacleData.Duration;
        wall.Duration *= -1f;

        BeatmapActionContainer.AddAction(
            new BeatmapObjectModifiedAction(wall, obs.ObjectData, obs.ObjectData),
            true);
    }
}
