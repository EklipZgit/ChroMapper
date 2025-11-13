namespace Editor.Environments.Structures.Components
{
    public class TubeBloomPrePassLightWithIdComponent : EnvironmentComponent<LightingObject>
    {
        public TubeBloomPrePassLightComponent TubeBloomPrePassLight;
        public ChromaLightComponent ChromaLight;

        public override void CopyTo(LightingObject target)
        {
        }
    }

    public class TubeBloomPrePassLightComponent
    {
        public float ColorAlphaMultiplier = 1f;
        public float BloomFogIntensityMultiplier = 1f;
        public float TubeLength = 1f;
        public float TubeWidth = 1f;
        public float Center = 1f;
        public float Height = 1f;
        public float StartAlpha = 0f;
        public float EndAlpha = 1f;
        public float LightWidthMultiplier = 1f;
        public bool UseCollision = false;
        public string ParametricBoxId = "";
    }
}
