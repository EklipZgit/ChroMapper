using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

public class BongoCat : MonoBehaviour
{
    [SerializeField] private GridViewController gridViewController;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private BongoCatPreset[] bongoCats;
    [SerializeField] private GridLane lane;

    [SerializeField] private bool armL;
    [SerializeField] private bool armR;

    private BongoCatPreset selectedBongoCat;

    private float armLTimeout;
    private float armRTimeout;

    private void Start()
    {
        selectedBongoCat = bongoCats[0];
        Settings.NotifyBySettingName(nameof(BongoCat), UpdateBongoCatState);
        gridViewController.OnGridViewUpdated += UpdatePosition;
        UpdateBongoCatState(Settings.Instance.BongoCat);
    }

    private void OnDestroy()
    {
        Settings.ClearSettingNotifications(nameof(Settings.BongoCat));
        gridViewController.OnGridViewUpdated -= UpdatePosition;
    }

    private void Update()
    {
        armLTimeout -= Time.deltaTime;
        armRTimeout -= Time.deltaTime;
        if (armLTimeout < 0) armL = false;
        if (armRTimeout < 0) armR = false;

        spriteRenderer.sprite = armL
            ? armR
                ? selectedBongoCat.LeftDownRightDown
                : selectedBongoCat.LeftDownRightUp
            : armR
                ? selectedBongoCat.LeftUpRightDown
                : selectedBongoCat.LeftUpRightUp;
    }

    private void UpdateBongoCatState(object obj)
    {
        switch (Settings.Instance.BongoCat)
        {
            case -1:
                spriteRenderer.enabled = enabled = false;
                spriteRenderer.gameObject.SetActive(false);
                break;
            default:
                selectedBongoCat = bongoCats[Settings.Instance.BongoCat];
                spriteRenderer.enabled = enabled = true;
                spriteRenderer.gameObject.SetActive(true);
                break;
        }

        UpdatePosition();
    }

    private void UpdatePosition()
    {
        transform.localPosition = new Vector3(
            transform.localPosition.x,
            lane.Height + lane.XYOffset.y + selectedBongoCat.YOffset,
            -0.002f);

        transform.localScale = selectedBongoCat.Scale;
    }

    public void TriggerArm(BaseNote note, NoteGridContainer container)
    {
        //Ignore bombs here to improve performance.
        if (Settings.Instance.BongoCat == -1 || note.Type == (int)NoteType.Bomb) return;

        // TODO(Caeden): This can be optimized:
        //   - Pass note idx through the caller (DingOnNotePassingGrid? should be a direct callback subscriber tbh)
        //   - Manually march forward until the next object that matches our predicate is found
        var next = container.MapObjects.Find(x => x.JsonTime > note.JsonTime && x.Type == note.Type);

        var timer = 0.125f;
        if (next is not null)
        {
            // clamp to accommodate sliders and long gaps between notes
            var half = (next.SongBpmTime - note.SongBpmTime)
                * 60f
                / BeatSaberSongContainer.Instance.Info.BeatsPerMinute
                / 2f;
            timer = Mathf.Clamp(half, 0.05f, 0.2f);
        }

        switch (note.Type)
        {
            case (int)NoteType.Red:
                armL = true;
                armLTimeout = timer;
                break;
            case (int)NoteType.Blue:
                armR = true;
                armRTimeout = timer;
                break;
        }
    }
}
