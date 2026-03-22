using Beatmap.Base;

public class GLSGroupFloatFXPlacement : GLSGroupPlacement<BaseVfxEventEventBoxGroup, GLSGroupFloatFXGridContainer>
{
    public override bool CanPlace => base.CanPlace && GlsEventTrack.TrackDefinition.FloatFXTrack;

    protected override BaseVfxEventEventBoxGroup GenerateOriginalData() =>
        new() { Boxes = new() { new BaseVfxEventEventBox { Events = new[] { new BaseFxEventFloat() } } } };
}
