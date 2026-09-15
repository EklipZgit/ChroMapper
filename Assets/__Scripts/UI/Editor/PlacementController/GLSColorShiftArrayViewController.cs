using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GLSColorShiftArrayViewController : MonoBehaviour
{
    [SerializeField] private GLSColorShiftRowView rowTemplate;
    [SerializeField] private Transform listRoot;
    [SerializeField] private ButtonComponent addButton;

    private Action<string[]> onShiftsChanged;
    private bool suppressNotify;
    private readonly List<GLSColorShiftRowView> rows = new();

    private void Awake()
    {
        if (addButton != null)
        {
            addButton.OnClick(HandleAddClicked);
        }
        if (rowTemplate != null)
        {
            rowTemplate.gameObject.SetActive(false);
        }
    }

    public void Initialize(Action<string[]> onChanged)
    {
        onShiftsChanged = onChanged;
    }

    public void SetShifts(string[] shifts)
    {
        suppressNotify = true;
        try
        {
            ClearRows();
            if (shifts != null)
            {
                foreach (var s in shifts)
                {
                    CreateRow(s);
                }
            }
        }
        finally
        {
            suppressNotify = false;
        }
    }

    public string[] GetShifts() => rows.Select(r => r.GetDistribution()).ToArray();

    private void ClearRows()
    {
        foreach (var r in rows)
        {
            if (r != null) Destroy(r.gameObject);
        }
        rows.Clear();
    }

    private void HandleAddClicked()
    {
        CreateRow("h,0,lin");
        NotifyChanged();
    }

    private void CreateRow(string distribution)
    {
        if (rowTemplate == null || listRoot == null)
        {
            Debug.LogError("[GLSColorShiftArrayView] rowTemplate/listRoot not wired. No auto-creation.");
            return;
        }

        var row = Instantiate(rowTemplate, listRoot);
        row.gameObject.SetActive(true);
        row.SetDistribution(distribution);
        row.OnDistributionChanged += _ => { if (!suppressNotify) NotifyChanged(); };
        row.OnRemove += r =>
        {
            rows.Remove(r);
            Destroy(r.gameObject);
            if (!suppressNotify) NotifyChanged();
        };
        rows.Add(row);
    }

    private void NotifyChanged()
    {
        if (suppressNotify) return;
        var arr = GetShifts();
        onShiftsChanged?.Invoke(arr);
    }
}