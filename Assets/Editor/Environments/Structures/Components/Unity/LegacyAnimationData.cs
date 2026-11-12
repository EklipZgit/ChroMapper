using System;
using UnityEditor;
using UnityEngine;

public class LegacyAnimationData : EnvironmentComponentData<TimelineLegacyAnimation>
{
    public const string ClipsPath = "Assets/_Graphics/Animations/Environments";

    public float ClipFrameRate;
    public float ClipLength;
    public string ClipName;
    public float PlaybackSpeed = 1f;
    public float PhaseSeconds;
    public string WrapMode;

    public override void FillComponents(GameObject self, TimelineLegacyAnimation comp, CreateContainer container)
    {
        if (string.IsNullOrEmpty(ClipName))
        {
            comp.enabled = false;
            Debug.LogWarning($"{container.EnvironmentId}/{self.name} has no exported Animation.clipName. Keeping its static pose until the environment export is updated.");
            return;
        }

        var path = PathUtils.Combine(ClipsPath, ClipName + ".anim");
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
            throw new InvalidOperationException($"Native animation asset is missing: {path}.");

        if (Mathf.Abs(clip.length - ClipLength) > 0.0001f || clip.wrapMode.ToString() != WrapMode)
            throw new InvalidOperationException($"Native animation asset does not match the exported length/wrap mode: {path}.");

        comp.Clip = clip;
        comp.PlaybackSpeed = PlaybackSpeed;
        comp.PhaseSeconds = PhaseSeconds;
        comp.enabled = IsEnabled;
    }
}
