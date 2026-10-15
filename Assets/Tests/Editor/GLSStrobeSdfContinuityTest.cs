using System.Collections.Generic;
using Beatmap.Enums;
using NUnit.Framework;
using UnityEngine;

namespace Tests.Editor
{
    public class GLSStrobeSdfContinuityTest
    {
        private sealed class ReferenceCurve
        {
            private readonly Vector2[] points;

            public ReferenceCurve(EaseType easing, float pulseWidth)
            {
                var curve = Easing.FromID((int)easing);
                GLSStrobePulseWidthIconTest.GetCurveRange(curve, out var minimum, out var maximum);
                var path = new List<Vector2>();
                Vector2 Point(float phase, float value)
                {
                    return new Vector2(13f + (258f * phase), 13f + (102f * (value - minimum) / (maximum - minimum)));
                }

                if (pulseWidth == 0f)
                {
                    path.Add(Point(0, 0));
                    path.Add(Point(0, 1));
                }
                else
                {
                    for (var i = 0; i <= 2048; i++)
                    {
                        var progress = i / 2048f;
                        path.Add(Point(pulseWidth * progress, curve(progress)));
                    }
                }

                if (pulseWidth == 1f)
                {
                    path.Add(Point(1, 0));
                }
                else
                {
                    for (var i = 0; i <= 2048; i++)
                    {
                        var progress = i / 2048f;
                        path.Add(Point(pulseWidth + ((1f - pulseWidth) * progress), 1f - curve(progress)));
                    }
                }

                points = path.ToArray();
            }

            public float Distance(Vector2 coordinate)
            {
                // Only segments within the stroke's horizontal reach can affect these coverage assertions.
                var minimumX = coordinate.x - 14f;
                var maximumX = coordinate.x + 14f;
                var lower = 0;
                var upper = points.Length;
                while (lower < upper)
                {
                    var middle = lower + ((upper - lower) / 2);
                    if (points[middle].x < minimumX)
                    {
                        lower = middle + 1;
                    }
                    else
                    {
                        upper = middle;
                    }
                }

                var closest = float.PositiveInfinity;
                for (var i = Mathf.Max(0, lower - 1); i < points.Length - 1 && points[i].x <= maximumX; i++)
                {
                    var start = points[i];
                    var delta = points[i + 1] - start;
                    var position = Mathf.Clamp01(Vector2.Dot(coordinate - start, delta) / Mathf.Max(delta.sqrMagnitude, 1e-12f));
                    var offset = coordinate - start - (position * delta);
                    closest = Mathf.Min(closest, offset.sqrMagnitude);
                }

                return Mathf.Sqrt(closest);
            }
        }

        [TestCase(EaseType.InOutCubic, 0.95f)]
        [TestCase(EaseType.OutCircular, 0.95f)]
        [TestCase(EaseType.OutElastic, 0.95f)]
        [TestCase(EaseType.BeatSaberInOutBounce, 0.95f)]
        public void PixelsBeyondTheStrokeDoNotGainWhiteEdgeStripes(EaseType easing, float pulseWidth)
        {
            var pixelOffset = new Vector2(-1.25f, 0.75f);
            var pixels = GLSStrobePulseWidthIconTest.RenderCurve(easing, pulseWidth, Color.green, pixelOffset);
            var reference = new ReferenceCurve(easing, pulseWidth);
            for (var y = 0; y < 256; y++)
            {
                for (var x = 0; x < 568; x++)
                {
                    var pixel = pixels[(y * 568) + x];
                    if (pixel.r <= 15)
                    {
                        continue;
                    }

                    var distance = reference.Distance(new Vector2(
                        (x + 0.5f + pixelOffset.x) * 0.5f,
                        (y + 0.5f + pixelOffset.y) * 0.5f));
                    Assert.That(distance, Is.LessThanOrEqualTo(12.75f),
                        $"{easing}, pulse width {pulseWidth}: stray white coverage at ({x}, {y}), distance={distance}.");
                }
            }
        }

        [TestCase(EaseType.OutBounce, 0.95f)]
        [TestCase(EaseType.BeatSaberInOutBounce, 0.95f)]
        [TestCase(EaseType.OutElastic, 0.95f)]
        [TestCase(EaseType.OutCircular, 0.97f)]
        public void PulseJoinHasNoDarkSeamInsideItsWhiteCore(EaseType easing, float pulseWidth)
        {
            var pixels = GLSStrobePulseWidthIconTest.RenderCurve(easing, pulseWidth, Color.gray);
            var reference = new ReferenceCurve(easing, pulseWidth);
            var split = 13f + (258f * pulseWidth);
            var minimumX = Mathf.Max(0, Mathf.FloorToInt((split - 20f) * 2f));
            var maximumX = Mathf.Min(568, Mathf.CeilToInt((split + 20f) * 2f));
            for (var y = 0; y < 256; y++)
            {
                for (var x = minimumX; x < maximumX; x++)
                {
                    var distance = reference.Distance(new Vector2((x + 0.5f) * 0.5f, (y + 0.5f) * 0.5f));
                    if (distance < 4.9f)
                    {
                        Assert.That(pixels[(y * 568) + x].r, Is.GreaterThanOrEqualTo(240),
                            $"{easing}, pulse width {pulseWidth}: dark seam near phase join at ({x}, {y}), distance={distance}.");
                    }
                }
            }
        }

        [TestCase(EaseType.InOutCubic, 0.95f)]
        [TestCase(EaseType.OutElastic, 0.95f)]
        [TestCase(EaseType.OutBounce, 0.95f)]
        [TestCase(EaseType.BeatSaberInOutBounce, 0.95f)]
        [TestCase(EaseType.InBounce, 0.05f)]
        [TestCase(EaseType.InOutBounce, 0.95f)]
        [TestCase(EaseType.BeatSaberInOutBounce, 0.97f)]
        [TestCase(EaseType.BeatSaberInOutBounce, 0.03f)]
        [TestCase(EaseType.OutBounce, 0.374f)]
        [TestCase(EaseType.OutBounce, 0.376f)]
        [TestCase(EaseType.OutBounce, 0.624f)]
        [TestCase(EaseType.OutBounce, 0.626f)]
        [TestCase(EaseType.BeatSaberInOutBounce, 0.374f)]
        [TestCase(EaseType.BeatSaberInOutBounce, 0.376f)]
        [TestCase(EaseType.BeatSaberInOutBounce, 0.624f)]
        [TestCase(EaseType.BeatSaberInOutBounce, 0.626f)]
        public void DenseCurveCoreAndBorderStayContinuous(EaseType easing, float pulseWidth)
        {
            var pixels = GLSStrobePulseWidthIconTest.RenderCurve(easing, pulseWidth, Color.gray);
            var reference = new ReferenceCurve(easing, pulseWidth);
            for (var y = 0; y < 256; y += 2)
            {
                for (var x = 0; x < 568; x += 2)
                {
                    var distance = reference.Distance(new Vector2((x + 0.5f) * 0.5f, (y + 0.5f) * 0.5f));
                    var pixel = pixels[(y * 568) + x];
                    if (distance < 4.9f)
                    {
                        Assert.That(pixel.r, Is.GreaterThanOrEqualTo(240),
                            $"{easing}, pulse width {pulseWidth}: dark seam inside white core at ({x}, {y}), distance={distance}.");
                    }
                    else if (distance > 7f && distance < 10.25f)
                    {
                        Assert.That(pixel.r, Is.LessThanOrEqualTo(15),
                            $"{easing}, pulse width {pulseWidth}: interrupted black outline at ({x}, {y}), distance={distance}.");
                    }
                }
            }
        }
    }
}
