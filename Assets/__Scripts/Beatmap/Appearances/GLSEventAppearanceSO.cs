using System.Globalization;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;

namespace Beatmap.Appearances
{
    [CreateAssetMenu(menuName = "Beatmap/Appearance/GLS Event Appearance SO", fileName = "GLSEventAppearanceSO")]
    public class GLSEventAppearanceSO : ScriptableObject
    {
        [SerializeField] private EventAppearanceSO eventAppearance;

        public void SetAppearance(
            GLSEventContainer e,
            bool final = true,
            bool boost = false)
        {
        }
    }
}
