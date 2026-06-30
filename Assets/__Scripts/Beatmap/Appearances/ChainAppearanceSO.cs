using System.Globalization;
using System.Text;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;

namespace Beatmap.Appearances
{
    [CreateAssetMenu(menuName = "Beatmap/Appearance/Chain Appearance SO", fileName = "ChainAppearanceSO")]
    public class ChainAppearanceSO : ScriptableObject
    {
        public Color RedColor { get; private set; } = DefaultColors.LeftNote;
        public Color BlueColor { get; private set; } = DefaultColors.RightNote;

        public void UpdateColor(Color red, Color blue)
        {
            RedColor = red;
            BlueColor = blue;
        }

        public void SetChainAppearance(ChainContainer chain)
        {
            switch (chain.ChainData.Color)
            {
                case (int)NoteColor.Red:
                    chain.SetColor(RedColor);
                    break;
                case (int)NoteColor.Blue:
                    chain.SetColor(BlueColor);
                    break;
            }

            SetText(chain);
            if (chain.ChainData.CustomColor != null) chain.SetColor((Color)chain.ChainData.CustomColor);
            chain.Animator.AttachToObject(chain.ChainData);
        }

        public void SetText(ChainContainer chain)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"S: {chain.ChainData.Squish.ToString(CultureInfo.InvariantCulture)}");
            sb.AppendLine($"C: {chain.ChainData.SliceCount.ToString()}");
            if (chain.ChainData.Rotation != 0 || chain.ChainData.TailRotation != 0)
            {
                sb.AppendLine($"HR: {chain.ChainData.Rotation.ToString()}");
                sb.AppendLine($"TR: {chain.ChainData.TailRotation.ToString()}");
            }

            chain.InfoText.SetText(sb.ToString());
        }
    }
}
