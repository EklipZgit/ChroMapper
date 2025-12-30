using UnityEngine;

public class LightObjectSprite : LightObject
{
    private BoostSprite boostSprite;
    private static readonly int mainTexId = Shader.PropertyToID("_MainTex");

    protected override void Start()
    {
        base.Start();
        if (Renderer is not SpriteRenderer spriteRenderer || boostSprite == null) return;
        boostSprite.Setup(spriteRenderer.sprite);
        Mpb.SetTexture(mainTexId, spriteRenderer.sprite.texture);
    }
}
