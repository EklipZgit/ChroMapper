using System;
using System.Collections.Generic;
using UnityEngine;

/*
 * Copied from Arti
 *
 * Copied from https://gist.github.com/Fonserbc/3d31a25e87fdaa541ddf
 * Functions taken from Tween.js - Licensed under the MIT license
 * at https://github.com/sole/tween.js
 */
public static class Easing
{
    /// <summary>
    ///     Maps the names found at https://easings.net/en to the matching easing functions.
    ///     Maps "easeLinear" to linear easing (x => x).
    /// </summary>
    public static Dictionary<string, Func<float, float>> ByName = new()
    {
        { "easeLinear", Linear },
        { "easeInQuad", Quadratic.In },
        { "easeOutQuad", Quadratic.Out },
        { "easeInOutQuad", Quadratic.InOut },
        { "easeInCubic", Cubic.In },
        { "easeOutCubic", Cubic.Out },
        { "easeInOutCubic", Cubic.InOut },
        { "easeInQuart", Quartic.In },
        { "easeOutQuart", Quartic.Out },
        { "easeInOutQuart", Quartic.InOut },
        { "easeInQuint", Quintic.In },
        { "easeOutQuint", Quintic.Out },
        { "easeInOutQuint", Quintic.InOut },
        { "easeInSine", Sinusoidal.In },
        { "easeOutSine", Sinusoidal.Out },
        { "easeInOutSine", Sinusoidal.InOut },
        { "easeInExpo", Exponential.In },
        { "easeOutExpo", Exponential.Out },
        { "easeInOutExpo", Exponential.InOut },
        { "easeInCirc", Circular.In },
        { "easeOutCirc", Circular.Out },
        { "easeInOutCirc", Circular.InOut },
        { "easeInBack", Back.In },
        { "easeOutBack", Back.Out },
        { "easeInOutBack", Back.InOut },
        { "easeInElastic", Elastic.In },
        { "easeOutElastic", Elastic.Out },
        { "easeInOutElastic", Elastic.InOut },
        { "easeInBounce", Bounce.In },
        { "easeOutBounce", Bounce.Out },
        { "easeInOutBounce", Bounce.InOut },
        { "easeStep", Step }
    };

    /// <summary>
    ///     Maps UI-friendly display names to the names found at https://easings.net/en.
    ///     Used in conjunction with <seealso cref="ByName" /> to obtain the Easing function from a display name.
    /// </summary>
    public static Dictionary<string, string> DisplayNameToInternalName = new()
    {
        { "Linear", "easeLinear" },
        { "Quadratic In", "easeInQuad" },
        { "Quadratic Out", "easeOutQuad" },
        { "Quadratic In/Out", "easeInOutQuad" },
        { "Cubic In", "easeInCubic" },
        { "Cubic Out", "easeOutCubic" },
        { "Cubic In/Out", "easeInOutCubic" },
        { "Quartic In", "easeInQuart" },
        { "Quartic Out", "easeOutQuart" },
        { "Quartic In/Out", "easeInOutQuart" },
        { "Quintic In", "easeInQuint" },
        { "Quintic Out", "easeOutQuint" },
        { "Quintic In/Out", "easeInOutQuint" },
        { "Sine In", "easeInSine" },
        { "Sine Out", "easeOutSine" },
        { "Sine In/Out", "easeInOutSine" },
        { "Exponential In", "easeInExpo" },
        { "Exponential Out", "easeOutExpo" },
        { "Exponential In/Out", "easeInOutExpo" },
        { "Circular In", "easeInCirc" },
        { "Circular Out", "easeOutCirc" },
        { "Circular In/Out", "easeInOutCirc" },
        { "Back In", "easeInBack" },
        { "Back Out", "easeOutBack" },
        { "Back In/Out", "easeInOutBack" },
        { "Elastic In", "easeInElastic" },
        { "Elastic Out", "easeOutElastic" },
        { "Elastic In/Out", "easeInOutElastic" },
        { "Bounce In", "easeInBounce" },
        { "Bounce Out", "easeOutBounce" },
        { "Bounce In/Out", "easeInOutBounce" },
        { "Step", "easeStep" }
    };

    /// <summary>
    ///     Maps the ID to easing found at https://easings.net/en to the matching easing functions.
    ///     Maps 0 to linear easing (x => x).
    /// </summary>
    public static readonly Dictionary<int, Func<float, float>> ByID = new()
    {
        { -1, Step },
        { 0, Linear },
        { 1, Quadratic.In },
        { 2, Quadratic.Out },
        { 3, Quadratic.InOut },
        { 4, Sinusoidal.In },
        { 5, Sinusoidal.Out },
        { 6, Sinusoidal.InOut },
        { 7, Cubic.In },
        { 8, Cubic.Out },
        { 9, Cubic.InOut },
        { 10, Quartic.In },
        { 11, Quartic.Out },
        { 12, Quartic.InOut },
        { 13, Quintic.In },
        { 14, Quintic.Out },
        { 15, Quintic.InOut },
        { 16, Exponential.In },
        { 17, Exponential.Out },
        { 18, Exponential.InOut },
        { 19, Circular.In },
        { 20, Circular.Out },
        { 21, Circular.InOut },
        { 22, Back.In },
        { 23, Back.Out },
        { 24, Back.InOut },
        { 25, Elastic.In },
        { 26, Elastic.Out },
        { 27, Elastic.InOut },
        { 28, Bounce.In },
        { 29, Bounce.Out },
        { 30, Bounce.InOut },
        { 100, Back.BeatSaberInOut },
        { 101, Elastic.BeatSaberInOut },
        { 102, Bounce.BeatSaberInOut }
    };

    /// <summary>
    ///     If an easing named <paramref name="name" /> exists, returns it.
    ///     Otherwise, returns <see cref="Linear(float)" />.
    ///     <seealso cref="ByName" />
    /// </summary>
    /// <param name="name">The name of the desired easing.</param>
    /// <returns>The desired easing, or <see cref="Linear(float)" /> if that easing doesn't exist.</returns>
    public static Func<float, float> Named(string name) => ByName.TryGetValue(name, out var easing) ? easing : Linear;

    /// <summary>
    ///     If an easing ID <paramref name="id" /> exists, returns it.
    ///     Otherwise, returns <see cref="Linear(float)" />.
    ///     <seealso cref="ByName" />
    /// </summary>
    /// <param name="id">The ID of the desired easing.</param>
    /// <returns>The desired easing, or <see cref="Linear(float)" /> if that easing doesn't exist.</returns>
    public static Func<float, float> FromID(int id) => ByID.TryGetValue(id, out var easing) ? easing : Linear;

    /// <summary>
    /// Returns the shader ID for a given easing.
    /// </summary>
    /// <param name="easingId">Internal easing ID (what Chroma uses)</param>
    /// <returns>Numerical ID used for the basic gradient shader.</returns>
    public static int EasingShaderId(string easingId)
    {
        var i = 0;
        foreach (var easing in ByName.Keys)
        {
            if (easing == easingId) return i;
            i++;
        }

        return 0;
    }

    public static float Linear(float k) => k;

    public static float Step(float k) => Mathf.Floor(k);

    public static class Quadratic
    {
        public static float In(float k) => k * k;

        public static float Out(float k) => k * (2f - k);

        public static float InOut(float k)
        {
            return (k *= 2f) < 1f
                ? 0.5f * k * k
                : -0.5f * (((k -= 1f) * (k - 2f)) - 1f);
        }
    }

    public static class Cubic
    {
        public static float In(float k) => k * k * k;

        public static float Out(float k) => 1f + ((k -= 1f) * k * k);

        public static float InOut(float k)
        {
            return (k *= 2f) < 1f
                ? 0.5f * k * k * k
                : 0.5f * (((k -= 2f) * k * k) + 2f);
        }
    }

    public static class Quartic
    {
        public static float In(float k) => k * k * k * k;

        public static float Out(float k) => 1f - ((k -= 1f) * k * k * k);

        public static float InOut(float k)
        {
            return (k *= 2f) < 1f
                ? 0.5f * k * k * k * k
                : -0.5f * (((k -= 2f) * k * k * k) - 2f);
        }
    }

    public static class Quintic
    {
        public static float In(float k) => k * k * k * k * k;

        public static float Out(float k) => 1f + ((k -= 1f) * k * k * k * k);

        public static float InOut(float k)
        {
            return (k *= 2f) < 1f
                ? 0.5f * k * k * k * k * k
                : 0.5f * (((k -= 2f) * k * k * k * k) + 2f);
        }
    }

    public static class Sinusoidal
    {
        public static float In(float k) => 1f - Mathf.Cos(k * Mathf.PI / 2f);

        public static float Out(float k) => Mathf.Sin(k * Mathf.PI / 2f);

        public static float InOut(float k) => 0.5f * (1f - Mathf.Cos(Mathf.PI * k));
    }

    public static class Exponential
    {
        public static float In(float k) => k == 0f ? 0f : Mathf.Pow(1024f, k - 1f);

        public static float Out(float k) => k == 1f ? 1f : 1f - Mathf.Pow(2f, -10f * k);

        public static float InOut(float k)
        {
            if (k == 0f) return 0f;
            if (k == 1f) return 1f;
            if ((k *= 2f) < 1f) return 0.5f * Mathf.Pow(1024f, k - 1f);
            return 0.5f * (-Mathf.Pow(2f, -10f * (k - 1f)) + 2f);
        }
    }

    public static class Circular
    {
        public static float In(float k) => 1f - Mathf.Sqrt(1f - (k * k));

        public static float Out(float k) => Mathf.Sqrt(1f - ((k -= 1f) * k));

        public static float InOut(float k)
        {
            return (k *= 2f) < 1f
                ? -0.5f * (Mathf.Sqrt(1f - (k * k)) - 1)
                : 0.5f * (Mathf.Sqrt(1f - ((k -= 2f) * k)) + 1f);
        }
    }

    public static class Elastic
    {
        public static float In(float k)
        {
            if (k == 0) return 0;
            if (k == 1) return 1;
            return -Mathf.Pow(2f, 10f * (k -= 1f)) * Mathf.Sin((k - 0.1f) * (2f * Mathf.PI) / 0.4f);
        }

        public static float Out(float k)
        {
            if (k == 0) return 0;
            if (k == 1) return 1;
            return (Mathf.Pow(2f, -10f * k) * Mathf.Sin((k - 0.1f) * (2f * Mathf.PI) / 0.4f)) + 1f;
        }

        public static float InOut(float k)
        {
            return (k *= 2f) < 1f
                ? -0.5f * Mathf.Pow(2f, 10f * (k -= 1f)) * Mathf.Sin((k - 0.1f) * (2f * Mathf.PI) / 0.4f)
                : (Mathf.Pow(2f, -10f * (k -= 1f)) * Mathf.Sin((k - 0.1f) * (2f * Mathf.PI) / 0.4f) * 0.5f) + 1f;
        }

        public static float BeatSaberInOut(float t)
        {
            if (t < 0.3f) return 37.037f * t * t * t;
            return (Mathf.Pow(2f, -10f * (t - 0.2f)) * Mathf.Sin(t * 10f * (MathF.PI * 2f / 3f))) + 1f;
        }
    }

    public static class Back
    {
        private static readonly float s = 1.70158f;
        private static readonly float s2 = 2.5949095f;

        public static float In(float k) => k * k * (((s + 1f) * k) - s);

        public static float Out(float k) => ((k -= 1f) * k * (((s + 1f) * k) + s)) + 1f;

        public static float InOut(float k)
        {
            if ((k *= 2f) < 1f) return 0.5f * (k * k * (((s2 + 1f) * k) - s2));
            return 0.5f * (((k -= 2f) * k * (((s2 + 1f) * k) + s2)) + 2f);
        }

        public static float BeatSaberInOut(float t)
        {
            if (t < 0.517f) return 5.014f * t * t * t;

            return 1f
                + s2 * Mathf.Pow(1.665f * (t + -0.4f) - 1f, 3f)
                + s * Mathf.Pow(1.665f * (t + -0.4f) - 1f, 2f);
        }
    }

    public static class Bounce
    {
        public static float In(float k) => 1f - Out(1f - k);

        public static float Out(float k)
        {
            if (k < 1f / 2.75f) return 7.5625f * k * k;
            if (k < 2f / 2.75f) return (7.5625f * (k -= 1.5f / 2.75f) * k) + 0.75f;
            if (k < 2.5f / 2.75f) return (7.5625f * (k -= 2.25f / 2.75f) * k) + 0.9375f;
            return (7.5625f * (k -= 2.625f / 2.75f) * k) + 0.984375f;
        }

        public static float InOut(float k)
        {
            if (k < 0.5f) return In(k * 2f) * 0.5f;
            return (Out((k * 2f) - 1f) * 0.5f) + 0.5f;
        }

        public static float BeatSaberInOut(float t)
        {
            return t switch
            {
                < 0.72727275f and < 0.36363637f => 20.796f * t * t * t,
                < 0.72727275f => 7.5625f * (t -= 0.54545456f) * t + 0.75f,
                < 0.90909094f => 7.5625f * (t -= 0.8181818f) * t + 0.9375f,
                _ => 7.5625f * (t -= 21f / 22f) * t + 63f / 64f
            };
        }
    }
}
