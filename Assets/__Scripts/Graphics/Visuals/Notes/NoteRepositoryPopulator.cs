using System.Linq;
using TMPro;
using UnityEngine;

public class NoteRepositoryPopulator : MonoBehaviour
{
    public VisualRepositorySO Repository;
    public TMP_Dropdown Dropdown;

    public void Start()
    {
        Dropdown.ClearOptions();
        Dropdown.AddOptions(Repository.NoteModelsByName.Keys.ToList());
    }
}
