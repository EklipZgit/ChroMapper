using Beatmap.Base;

public class GLSGroupColorPlacement : GLSGroupPlacement<BaseLightColorEventBoxGroup, GLSGroupColorGridContainer>
{
    public override bool CanPlace => base.CanPlace && GlsEventTrack.TrackDefinition.ColorTrack;

    protected override BaseLightColorEventBoxGroup GenerateOriginalData() =>
        new()
        {
            Boxes = new()
            {
                new BaseLightColorEventBox { Events = new[] { new BaseLightColorBase { Brightness = 1f } } }
            }
        };
}
