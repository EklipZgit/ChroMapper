using System.Globalization;
using System.Text;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;

namespace Beatmap.Appearances
{
    [CreateAssetMenu(menuName = "Beatmap/Appearance/Arc Appearance SO", fileName = "ArcAppearanceSO")]
    public class ArcAppearanceSO : ScriptableObject
    {
        public Color RedColor { get; private set; } = DefaultColors.LeftNote;
        public Color BlueColor { get; private set; } = DefaultColors.RightNote;

        public void UpdateColor(Color red, Color blue)
        {
            RedColor = red;
            BlueColor = blue;
        }

        public void SetArcAppearance(ArcContainer arc)
        {
            switch (arc.ArcData.Color)
            {
                case (int)NoteColor.Red:
                    arc.SetColor(RedColor);
                    break;
                case (int)NoteColor.Blue:
                    arc.SetColor(BlueColor);
                    break;
            }

            if (arc.ArcData.CustomColor != null) arc.SetColor((Color)arc.ArcData.CustomColor);
            SetText(arc);
            arc.Animator.AttachToObject(arc.ArcData);
        }

        public void SetText(ArcContainer arc)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"H: {arc.ArcData.HeadControlPointLengthMultiplier.ToString(CultureInfo.InvariantCulture)}");
            sb.AppendLine($"T: {arc.ArcData.TailControlPointLengthMultiplier.ToString(CultureInfo.InvariantCulture)}");
            if (arc.ArcData.Rotation != 0 || arc.ArcData.TailRotation != 0)
            {
                sb.AppendLine($"HR: {arc.ArcData.Rotation.ToString()}");
                sb.AppendLine($"TR: {arc.ArcData.TailRotation.ToString()}");
            }

            arc.InfoText.SetText(sb.ToString());
        }
    }
}
