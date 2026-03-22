using System.Linq;
using Beatmap.Base;

public class
    GLSGroupRotationPlacement : GLSGroupPlacement<BaseLightRotationEventBoxGroup, GLSGroupRotationGridContainer>
{
    public override bool CanPlace => base.CanPlace && GlsEventTrack.TrackDefinition.RotationTracks.Any(x => x);

    protected override BaseLightRotationEventBoxGroup GenerateOriginalData() =>
        new() { Boxes = new() { new BaseLightRotationEventBox { Events = new[] { new BaseLightRotationBase() } } } };
}
