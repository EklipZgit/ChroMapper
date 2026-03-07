using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Graphics/VisualSettings")]
// TODO: expand this to be proper model selection
public class VisualSettingsSO : ScriptableObject
{
    public event Action OnBlockModelChanged;
    public event Action OnEventModelChanged;
    public event Action OnNoteModelChanged;
    public event Action OnBombModelChanged;
    public event Action OnChainHeadModelChanged;
    public event Action OnChainLinkModelChanged;

    private static readonly Dictionary<(bool simple, bool solid), string> chainLinkModels = new()
    {
        { (false, false), "CM_Chain" },
        { (false, true), "CM_Chain_Solid" },
        { (true, false), "CM_Chain_Simple" },
        { (true, true), "CM_Chain_Solid_Simple" }
    };

    private readonly string[] eventModels =
    {
        "CM_Event_Block", "CM_Event_Pyramid", "CM_Event_Pyramid_Flat", "CM_Event_Node"
    };

    public void OnEnable()
    {
        Settings.NotifyBySettingName("BlockModel", HandleBlockModelChanged);
        Settings.NotifyBySettingName("EventModel", HandleEventModelChanged);
        Settings.NotifyBySettingName("SimpleBlocks", HandleNoteModelChanged);
        // Settings.NotifyBySettingName("SimpleBlocks", HandleBombModelChanged);
        Settings.NotifyBySettingName("SimpleBlocks", HandleChainHeadModelChanged);
        Settings.NotifyBySettingName("SimpleBlocks", HandleChainLinkModelChanged);
        Settings.NotifyBySettingName("SolidChainLink", HandleChainLinkModelChanged);
    }

    public void OnDisable()
    {
        Settings.ClearSettingNotifications("BlockModel");
        Settings.ClearSettingNotifications("EventModel");
        Settings.ClearSettingNotifications("SimpleBlocks");
        Settings.ClearSettingNotifications("SolidChainLink");
    }

    private void HandleBlockModelChanged(object _) => OnBlockModelChanged?.Invoke();
    private void HandleEventModelChanged(object _) => OnEventModelChanged?.Invoke();
    private void HandleNoteModelChanged(object _) => OnNoteModelChanged?.Invoke();
    private void HandleBombModelChanged(object _) => OnBombModelChanged?.Invoke();
    private void HandleChainHeadModelChanged(object _) => OnChainHeadModelChanged?.Invoke();
    private void HandleChainLinkModelChanged(object _) => OnChainLinkModelChanged?.Invoke();

    public string GetBlockModel() => "CM_Block";

    public string GetEventModel() => eventModels[(int)Settings.Instance.EventModel];

    public string GetNoteModel() => Settings.Instance.SimpleBlocks ? "CM_Note_Simple" : "CM_Note";

    public string GetBombModel() => "CM_Bomb";

    public string GetChainHeadModel() => Settings.Instance.SimpleBlocks ? "CM_Note_Chain_Simple" : "CM_Note_Chain";

    public string GetChainLinkModel() =>
        chainLinkModels[(Settings.Instance.SimpleBlocks, Settings.Instance.SolidChainLink)];
}
