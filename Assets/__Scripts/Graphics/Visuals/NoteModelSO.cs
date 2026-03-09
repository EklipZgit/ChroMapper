using CustomNotes;
using UnityEngine;

[CreateAssetMenu(fileName = "NoteModelSO", menuName = "Graphics/Create Note Model")]
public class NoteModelSO : ScriptableObject
{
    public string FileName;
    public AssetBundle AssetBundle;
    public NoteDescriptor Descriptor;

    [Header("Prefab")] public VisualModelSO NoteLeft;
    public VisualModelSO NoteRight;
    public VisualModelSO NoteDotLeft;
    public VisualModelSO NoteDotRight;
    public VisualModelSO NoteBomb;
    public VisualModelSO BurstSliderLeft;
    public VisualModelSO BurstSliderRight;
    public VisualModelSO BurstSliderHeadLeft;
    public VisualModelSO BurstSliderHeadRight;
    public VisualModelSO BurstSliderHeadDotLeft;
    public VisualModelSO BurstSliderHeadDotRight;

    public static NoteModelSO Create(AssetBundle assetBundle)
    {
        var so = CreateInstance<NoteModelSO>();
        so.AssetBundle = assetBundle;

        var prefab = assetBundle.LoadAsset<GameObject>("assets/_customnote.prefab");
        if (prefab == null) return null;

        so.Descriptor = prefab.GetComponent<NoteDescriptor>();
        so.name = so.Descriptor.NoteName;
        if (string.IsNullOrEmpty(so.name)) so.name = assetBundle.name;

        foreach (var comp in prefab.GetComponentsInChildren<Renderer>())
        foreach (var mat in comp.sharedMaterials)
        {
            if (mat == null) continue;
            var shader = Shader.Find(mat.shader.name);
            if (shader != null && shader.isSupported)
                mat.shader = shader;
            else if (Settings.Instance.ShaderCompatibility) mat.shader = Shader.Find("ChroMapper/Object/Note");
        }

        so.NoteLeft = VisualModelSO.Create(prefab.transform.Find("NoteLeft").gameObject, so.name);
        so.NoteRight = VisualModelSO.Create(prefab.transform.Find("NoteRight").gameObject, so.name);
        var noteDotLeftTransform = prefab.transform.Find("NoteDotLeft");
        var noteDotRightTransform = prefab.transform.Find("NoteDotRight");
        so.NoteDotLeft = noteDotLeftTransform != null
            ? VisualModelSO.Create(noteDotLeftTransform.gameObject, so.name)
            : so.NoteLeft;
        so.NoteDotRight = noteDotRightTransform != null
            ? VisualModelSO.Create(noteDotRightTransform.gameObject, so.name)
            : so.NoteRight;
        var bomb = prefab.transform.Find("NoteBomb");
        if (bomb != null) so.NoteBomb = VisualModelSO.Create(bomb.gameObject, so.name);

        so.BurstSliderLeft = VisualModelSO.Create(
            GetBurstSlider(prefab, so.NoteDotLeft.Prefab, "BurstSliderLeft"),
            so.name);
        so.BurstSliderRight = VisualModelSO.Create(
            GetBurstSlider(prefab, so.NoteDotRight.Prefab, "BurstSliderRight"),
            so.name);

        var burstSliderHeadLeft = prefab.transform.Find("BurstSliderHeadLeft");
        var burstSliderHeadRight = prefab.transform.Find("BurstSliderHeadRight");
        so.BurstSliderHeadLeft = burstSliderHeadLeft != null
            ? VisualModelSO.Create(burstSliderHeadLeft.gameObject, so.name)
            : so.NoteLeft;
        so.BurstSliderHeadRight = burstSliderHeadRight != null
            ? VisualModelSO.Create(burstSliderHeadRight.gameObject, so.name)
            : so.NoteRight;

        var burstSliderHeadDotLeft = prefab.transform.Find("BurstSliderHeadDotLeft");
        var burstSliderHeadDotRight = prefab.transform.Find("BurstSliderHeadDotRight");
        so.BurstSliderHeadDotLeft =
            burstSliderHeadDotLeft != null ? VisualModelSO.Create(burstSliderHeadDotLeft.gameObject, so.name)
            : burstSliderHeadLeft != null  ? VisualModelSO.Create(burstSliderHeadLeft.gameObject, so.name)
                                             : so.NoteDotLeft;
        so.BurstSliderHeadDotRight =
            burstSliderHeadDotRight != null ? VisualModelSO.Create(burstSliderHeadDotRight.gameObject, so.name)
            : burstSliderHeadRight != null  ? VisualModelSO.Create(burstSliderHeadRight.gameObject, so.name)
                                              : so.NoteDotRight;

        ResetTransform(so.NoteLeft.Prefab);
        ResetTransform(so.NoteRight.Prefab);
        ResetTransform(so.NoteDotLeft.Prefab);
        ResetTransform(so.NoteDotRight.Prefab);
        if (so.NoteBomb != null) ResetTransform(so.NoteBomb.Prefab);
        ResetTransform(so.BurstSliderHeadLeft.Prefab);
        ResetTransform(so.BurstSliderHeadRight.Prefab);
        ResetTransform(so.BurstSliderHeadDotLeft.Prefab);
        ResetTransform(so.BurstSliderHeadDotRight.Prefab);

        ResetGameObject(so.NoteLeft);
        ResetGameObject(so.NoteRight);
        ResetGameObject(so.NoteDotLeft);
        ResetGameObject(so.NoteDotRight);
        if (so.NoteBomb != null) ResetGameObject(so.NoteBomb);
        ResetGameObject(so.BurstSliderLeft);
        ResetGameObject(so.BurstSliderRight);
        ResetGameObject(so.BurstSliderHeadLeft);
        ResetGameObject(so.BurstSliderHeadRight);
        ResetGameObject(so.BurstSliderHeadDotLeft);
        ResetGameObject(so.BurstSliderHeadDotRight);

        return so;

        void ResetGameObject(VisualModelSO vm)
        {
            if (vm == null || vm.Prefab == null) return;
            vm.DisableAux = so.Descriptor.DisableBaseNoteArrows;
            vm.Prefab.SetLayerRecursively(LayerMask.NameToLayer("Beatmap Object"));
        }

        void ResetTransform(GameObject go)
        {
            if (go == null) return;
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one * 0.4f;
        }

        GameObject GetBurstSlider(GameObject p, GameObject dP, string prefabName)
        {
            var t = p.transform.Find(prefabName);
            if (t != null)
            {
                ResetTransform(t.gameObject);
                return t.gameObject;
            }

            var burstSlider = new GameObject(prefabName);
            DontDestroyOnLoad(burstSlider);

            var burstSliderDot = Instantiate(dP, burstSlider.transform, true);
            burstSliderDot.transform.localPosition = Vector3.zero;

            var sliderScale = burstSliderDot.transform.localScale;
            var scale = sliderScale;
            scale.y = sliderScale.y / 4f;
            burstSliderDot.transform.localScale = scale;

            burstSlider.SetActive(false);
            ResetTransform(burstSlider);
            return burstSlider;
        }
    }
}
