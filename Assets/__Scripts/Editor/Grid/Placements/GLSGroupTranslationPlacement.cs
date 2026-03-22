using System.Linq;
using Beatmap.Base;

public class
    GLSGroupTranslationPlacement : GLSGroupPlacement<BaseLightTranslationEventBoxGroup,
    GLSGroupTranslationGridContainer>
{
    public override bool CanPlace => base.CanPlace && GlsEventTrack.TrackDefinition.TranslationTracks.Any(x => x);

    protected override BaseLightTranslationEventBoxGroup GenerateOriginalData() =>
        new()
        {
            Boxes = new() { new BaseLightTranslationEventBox { Events = new[] { new BaseLightTranslationBase() } } }
        };
}
