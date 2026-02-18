using System;
using UnityEngine;

namespace Beatmap.Info
{
    public class InfoColorScheme : IEquatable<InfoColorScheme>
    {
        [Obsolete("This property is used for v2 and v3 only.")]
        public bool UseOverride { get; set; }

        public string ColorSchemeName { get; set; }

        public bool OverrideNotes { get; set; }
        public Color SaberAColor { get; set; }
        public Color SaberBColor { get; set; }
        public Color ObstaclesColor { get; set; }

        public bool OverrideLights { get; set; }
        public Color EnvironmentColor0 { get; set; }
        public Color EnvironmentColor1 { get; set; }
        public Color? EnvironmentColorW { get; set; }
        public Color EnvironmentColor0Boost { get; set; }
        public Color EnvironmentColor1Boost { get; set; }
        public Color? EnvironmentColorWBoost { get; set; }

        public bool Equals(InfoColorScheme other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return UseOverride == other.UseOverride
                && ColorSchemeName == other.ColorSchemeName
                && OverrideNotes == other.OverrideNotes
                && SaberAColor.Equals(other.SaberAColor)
                && SaberBColor.Equals(other.SaberBColor)
                && ObstaclesColor.Equals(other.ObstaclesColor)
                && OverrideLights == other.OverrideLights
                && EnvironmentColor0.Equals(other.EnvironmentColor0)
                && EnvironmentColor1.Equals(other.EnvironmentColor1)
                && Nullable.Equals(EnvironmentColorW, other.EnvironmentColorW)
                && EnvironmentColor0Boost.Equals(other.EnvironmentColor0Boost)
                && EnvironmentColor1Boost.Equals(other.EnvironmentColor1Boost)
                && Nullable.Equals(EnvironmentColorWBoost, other.EnvironmentColorWBoost);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((InfoColorScheme)obj);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(UseOverride);
            hashCode.Add(ColorSchemeName);
            hashCode.Add(OverrideNotes);
            hashCode.Add(SaberAColor);
            hashCode.Add(SaberBColor);
            hashCode.Add(ObstaclesColor);
            hashCode.Add(OverrideLights);
            hashCode.Add(EnvironmentColor0);
            hashCode.Add(EnvironmentColor1);
            hashCode.Add(EnvironmentColorW);
            hashCode.Add(EnvironmentColor0Boost);
            hashCode.Add(EnvironmentColor1Boost);
            hashCode.Add(EnvironmentColorWBoost);
            return hashCode.ToHashCode();
        }
    }
}
