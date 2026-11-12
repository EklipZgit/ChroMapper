using Beatmap.Base;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Containers
{
    public sealed class NoteSpawnEffect : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Transform spriteTransform;
        [SerializeField] private BloomFogObject bloomFog;
        [System.Serializable]
        private sealed class AnimationParameters
        {
            public AnimationCurve LightIntensityCurve;
            public AnimationCurve SpriteXScaleCurve;
            public AnimationCurve SpriteYScaleCurve;
            public AnimationCurve TransparencyCurve;
            public float BloomFogIntensity;
        }

        [SerializeField] private AnimationParameters normalAnimation;
        [SerializeField] private AnimationParameters shortAnimation;
        private AnimationParameters animationParameters;

        internal const float Duration = 0.4f;
        private const string SpawnEffectKey = "spawnEffect";
        private const string LegacyDisableSpawnEffectKey = "_disableSpawnEffect";
        private const string SettingsKey = "_settings";
        private const string PlayerOptionsKey = "_playerOptions";
        private const string HideNoteSpawnEffectKey = "_hideNoteSpawnEffect";

        private NoteContainer note;
        private BeatmapRuntimeContext context;
        private float spawnBeat;
        private float spawnSeconds;
        private bool spawnEnabled;
        private bool anchored;
        private bool visible;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private Color spawnColor;
        private float spawnIntensity;

        internal SpriteRenderer SpriteRenderer => spriteRenderer;
        internal BloomFogObject BloomFog => bloomFog;
        internal float SpawnSeconds => spawnSeconds;

        internal void Bind(NoteContainer note, BeatmapRuntimeContext context)
        {
            this.note = note;
            this.context = context;
            animationParameters = context.Descriptor != null && context.Descriptor.SizeData.CeilingType == CeilingType.LowCeiling
                ? shortAnimation
                : normalAnimation;
            spawnIntensity = context.Descriptor != null ? context.Descriptor.BloomFogParams.NoteSpawnIntensity : 1f;
            bloomFog.IntensityMultiplier = animationParameters.BloomFogIntensity;
            var song = BeatSaberSongContainer.Instance;
            spawnEnabled = IsEnabled(note.NoteData, song.MapDifficultyInfo.CustomData, song.Map.RuntimeLevelCustomData);
            spawnBeat = note.NoteData.SpawnSongBpmTime;
            spawnSeconds = context.Atsc.GetSecondsFromBeat(spawnBeat);
            anchored = false;
            Hide();
        }

        internal static bool IsEnabled(BaseNote note, JSONNode difficultyCustomData, JSONNode levelCustomData)
        {
            // Chroma's explicit per-note value overrides the player's hide-spawn setting in either map version.
            if (note.CustomData?.GetValueOrDefault(SpawnEffectKey, null) is JSONBool enabled)
                return enabled.AsBool;

            if (note.CustomData?.GetValueOrDefault(LegacyDisableSpawnEffectKey, null) is JSONBool disabled)
                return !disabled.AsBool;

            var hide = difficultyCustomData?[SettingsKey]?[PlayerOptionsKey]?[HideNoteSpawnEffectKey];
            if (hide is not JSONBool)
                hide = levelCustomData?[SettingsKey]?[PlayerOptionsKey]?[HideNoteSpawnEffectKey];

            return !(hide is JSONBool ? hide.AsBool : Settings.Instance.HideNoteSpawnEffect);
        }

        internal void Evaluate()
        {
            var data = note.NoteData;
            if (spawnBeat != data.SpawnSongBpmTime)
            {
                spawnBeat = data.SpawnSongBpmTime;
                spawnSeconds = context.Atsc.GetSecondsFromBeat(spawnBeat);
                anchored = false;
            }
            var elapsed = context.Atsc.CurrentSeconds - spawnSeconds;
            if (!UIMode.PreviewMode || !spawnEnabled || elapsed < 0f || elapsed > Duration)
            {
                Hide();
                return;
            }
            if (!anchored || !context.Atsc.IsPlaying)
            {
                note.Animator.GetNoteSpawnPose(note, out spawnPosition, out spawnRotation, out spawnColor);
                spawnPosition -= new Vector3(0f, 0.15f, 0f);
                spawnColor *= spawnIntensity;
                anchored = true;
            }
            // The effect stays at the jump's world-space origin rather than following the flying note.
            transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            transform.localScale = Vector3.one;
            var progress = Mathf.Clamp01(elapsed / Duration);
            var spriteColor = spawnColor;
            spriteColor.a = animationParameters.TransparencyCurve.Evaluate(progress);
            spriteRenderer.color = spriteColor;
            var fogColor = spawnColor;
            fogColor.a = animationParameters.LightIntensityCurve.Evaluate(progress);
            bloomFog.SetColor(fogColor);
            spriteTransform.localScale = new Vector3(
                animationParameters.SpriteXScaleCurve.Evaluate(progress),
                animationParameters.SpriteYScaleCurve.Evaluate(progress),
                1f);
            if (!visible)
            {
                spriteRenderer.enabled = true;
                bloomFog.gameObject.SetActive(true);
                visible = true;
            }
        }

        internal void Hide()
        {
            if (!visible)
                return;

            spriteRenderer.enabled = false;
            bloomFog.gameObject.SetActive(false);
            visible = false;
        }
    }
}
