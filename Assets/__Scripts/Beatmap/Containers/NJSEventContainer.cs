using System.Globalization;
using Beatmap.Base;
using TMPro;
using UnityEngine;

namespace Beatmap.Containers
{
    public class NJSEventContainer : ObjectContainer
    {
        [SerializeField] private TextMeshProUGUI njsText;
        public BaseNJSEvent NJSData;

        public override BaseObject ObjectData
        {
            get => NJSData;
            set => NJSData = (BaseNJSEvent)value;
        }

        public static NJSEventContainer SpawnNJSEvent(BaseNJSEvent data, ref GameObject prefab)
        {
            var container = Instantiate(prefab).GetComponent<NJSEventContainer>();
            container.NJSData = data;
            return container;
        }

        public void UpdateNJSText()
        {
            var absoluteNjs = (BeatSaberSongContainer.Instance.MapDifficultyInfo.NoteJumpSpeed + NJSData.RelativeNJS)
                .ToString(CultureInfo.InvariantCulture);
            njsText.text = Easing.IDToShortName.TryGetValue(NJSData.Easing, out var ease)
                ? $"{absoluteNjs}\n{ease}"
                : absoluteNjs;
            njsText.enabled = NJSData.UsePrevious != 1;
        }

        public override void UpdateGridPosition()
        {
            transform.localPosition = new Vector3(0.5f, 0.5f, NJSData.SongBpmTime * EditorScaleController.EditorScale);
            UpdateNJSText();
            UpdateCollisionGroups();
        }
    }
}
