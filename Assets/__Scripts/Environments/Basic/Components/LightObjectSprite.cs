using UnityEngine;

public class LightObjectSprite : LightObject
{
    private BoostSprite boostSprite;
    private static readonly int mainTexId = Shader.PropertyToID("_MainTex");

    protected override void Start()
    {
        base.Start();
        if (Renderer is not SpriteRenderer spriteRenderer) return;
        boostSprite.Setup(spriteRenderer.sprite);
        Mpb.SetTexture(mainTexId, spriteRenderer.sprite.texture);
    }

    public override void UpdateBoostState(bool boost) =>
        Mpb.SetTexture(mainTexId, boostSprite.GetSprite(boost).texture);

    protected override Color ModifyColor(Color color) => color * Multiply;
}
