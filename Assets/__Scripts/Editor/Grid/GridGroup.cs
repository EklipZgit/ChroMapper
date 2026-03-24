using System;
using UnityEngine;

public class GridGroup : GridChild
{
    [SerializeField] private GridViewController childViewController;

    public override void OnValidate()
    {
        base.OnValidate();
        Size.w = 1f;
        LocalOffset = Vector3.zero;
        SetScaleNoNotify(Scale);
        if (HasController) Controller.NotifyChanged();
    }

    private void Start() => childViewController.OnGridViewUpdated += HandleGridViewUpdated;
    private void OnDestroy() => childViewController.OnGridViewUpdated -= HandleGridViewUpdated;

    private void HandleGridViewUpdated()
    {
        Lane = Math.Max(0, childViewController.GetMaxSize() - 1);
        childViewController.FlipOdd = Lane % 2 != 0;
    }
}
