using NUnit.Framework;
using Tests.Infrastructure;

namespace Tests.Editor
{
    public class TestRenderSettingsTest : TestBase
    {
        [Test]
        public void ScreenshotsDefaultToNoChromaticAberration()
        {
            Assert.That(Settings.TestRunnerSettings.ChromaticAberration, Is.False);
            Assert.That(Settings.Instance.ChromaticAberration, Is.False);
            Assert.That(new Settings().ChromaticAberration, Is.True,
                "Test defaults must not change the application's normal graphics defaults.");
        }

        [Test]
        public void ChromaticAberrationTestsCanOptInWithoutAffectingLaterCases()
        {
            Settings.ApplyOptionByName(nameof(Settings.ChromaticAberration), true);
            try
            {
                Assert.That(Settings.Instance.ChromaticAberration, Is.True);
            }
            finally
            {
                SetUpEditorMode();
            }
            Assert.That(Settings.Instance.ChromaticAberration, Is.False);
        }
    }
}
