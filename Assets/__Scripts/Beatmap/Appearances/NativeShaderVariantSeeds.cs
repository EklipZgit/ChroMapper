using UnityEngine;

namespace Beatmap.Appearances
{
    public sealed class NativeShaderVariantSeeds : ScriptableObject
    {
        // Serialized materials retain combinations spanning vertex-only and fragment-only keywords.
        public Material[] Materials;
    }
}
