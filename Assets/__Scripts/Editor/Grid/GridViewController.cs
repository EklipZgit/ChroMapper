using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridViewController : MonoBehaviour
{
    private static Dictionary<int, List<GridChild>> allChildren = new();

    [SerializeField] private GridRotationController gridRotationController;

    [SerializeField] private int centerOffset;

    private static EditingMode editingMode = EditingMode.Gameplay;

    public static EditingMode EditingMode
    {
        get => editingMode;
        set
        {
            editingMode = value;
            NotifyChanged();
        }
    }

    public static event Action OnGridViewUpdated;

    private void Awake() => gridRotationController.OnObjectRotationChanged += NotifyChanged;
    private void OnEnable() => NotifyChanged();
    private void OnDestroy()
    {
        gridRotationController.OnObjectRotationChanged -= NotifyChanged;
        allChildren.Clear();
    }

    private static void UpdateGrid()
    {
        var activeChildren = new Dictionary<int, List<GridChild>>();

        foreach (var (order, children) in from child in allChildren from childViewable in child.Value select child)
        {
            foreach (var child in children)
            {
                if (child.ViewableMode.HasFlag(EditingMode) && child.gameObject.activeSelf)
                {
                    child.transform.localPosition = Vector3.zero;
                    if (activeChildren.ContainsKey(order))
                        activeChildren[order].Add(child);
                    else
                        activeChildren.Add(order, new List<GridChild> { child });
                }
                else
                    child.transform.localPosition = new Vector3(0, 69420, 69420);
            }
        }

        float childX = 0;
        if (activeChildren.Any(x => x.Key < 0))
        {
            if (activeChildren.TryGetValue(0, out var centerGridChildren))
                childX -= centerGridChildren.Max(x => x.Lane) / 2f;
            foreach (var (_, child) in activeChildren.Where(x => x.Key < 0))
                childX -= Mathf.Ceil(child.Max(x => x.Lane)) + 1;
        }

        var isOdd = false;
        if (activeChildren.TryGetValue(0, out var centerGrid)) isOdd = centerGrid.Max(x => x.Lane) % 2 != 0;

        foreach (var (order, children) in activeChildren)
        {
            children.RemoveAll(x => x == null);
            foreach (var child in children)
            {
                if (child is GridLane lane)
                    lane.OddLaneOffset = isOdd;

                child.transform.eulerAngles = new Vector3(
                    child.transform.eulerAngles.x,
                    child.transform.parent.eulerAngles.y,
                    child.transform.eulerAngles.z);
                var x = childX + child.LocalOffset.x;
                var side = child.transform.parent.right.normalized * x;
                var up = child.transform.parent.up.normalized * child.LocalOffset.y;
                var forward = child.transform.parent.forward.normalized * child.LocalOffset.z;
                var total = side + up + forward;
                child.transform.localPosition = total;
            }

            childX += Mathf.Ceil(children.Any() ? children.Max(x => x.Lane) + 1 : 0);
        }
    }

    public static int GetSizeForOrder(int order)
    {
        return allChildren.TryGetValue(order, out var children)
            ? Mathf.CeilToInt(
                children.Any() ? children.Where(x => x.ViewableMode.HasFlag(EditingMode)).Max(x => x.Lane) : 0)
            : 0;
    }

    public static void RegisterChild(GridChild child)
    {
        if (allChildren.TryGetValue(child.Order, out var grids))
            grids.Add(child);
        else
            allChildren[child.Order] = new List<GridChild> { child };
        NotifyChanged();
    }

    public static void DeregisterChild(GridChild child)
    {
        if (!allChildren.TryGetValue(child.Order, out var grids)) return;
        grids.Remove(child);
        if (grids.Count != 0) return;
        allChildren.Remove(child.Order);
        NotifyChanged();
    }

    public static void NotifyChanged()
    {
        RefreshChildDictionary();
        UpdateGrid();
        OnGridViewUpdated?.Invoke();
    }

    private static void RefreshChildDictionary()
    {
        allChildren = allChildren
            .SelectMany(x => x.Value)
            .GroupBy(x => x.Order)
            .OrderBy(x => x.Key)
            .ToDictionary(x => x.Key, x => x.ToList());
    }
}
