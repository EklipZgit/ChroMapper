using UnityEngine;

public class CutoutNoiseTextureHelper : MonoBehaviour
{
    // this is a stupidly simple script but this is just to keep me sane
    // i dont want to set the cutout texture manually in each material
    [SerializeField]
    private Texture3D noiseTexture;

    [SerializeField]
    private string propertyName;

    private void Start()
    {
        Shader.SetGlobalTexture(propertyName, noiseTexture);
    }

}
