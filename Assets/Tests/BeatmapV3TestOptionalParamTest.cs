using NUnit.Framework;
using Tests.Util;
using TestsEditMode;

namespace Tests
{
    public class BeatmapV3OptionalParamTest : TestBase
    {
        [Test]
        public void DoTheTest()
        {
            // Including EditMode test here in PlayMode so the pipeline runs the tests as well.
            new BeatmapV3OptionalParamTestEditMode().TestEverything();
        }
    }
}