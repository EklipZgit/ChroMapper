using System.Linq;
using TMPro;
using UnityEngine;

public class NoteRepositoryPopulator : MonoBehaviour
{
    public VisualRepositorySO Repository;
    public TMP_Dropdown Dropdown;

    public void Start() => PopulateModelList();

    public void OnEnable()
    {
        Repository.NoteModelListChanged += PopulateModelList;
        if (CustomNotesLoader.Instance != null)
            CustomNotesLoader.Instance.Refresh();
    }

    public void OnDisable() => Repository.NoteModelListChanged -= PopulateModelList;

    private void PopulateModelList()
    {
        Dropdown.ClearOptions();
        Dropdown.AddOptions(Repository.NoteModelNames.ToList());
        var selectedIndex = Dropdown.options.FindIndex(option => option.text == Settings.Instance.NoteModels);
        Dropdown.SetValueWithoutNotify(Mathf.Max(0, selectedIndex));
    }
}
