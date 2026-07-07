using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Tests.Util
{
    public abstract class TestBase
    {
        [UnityOneTimeSetUp]
        public IEnumerator LoadMap()
        {
            yield return TestUtils.LoadMap(3);
            yield return OnMapLoaded();
        }

        protected virtual IEnumerator OnMapLoaded()
        {
            yield break;
        }

        [OneTimeTearDown]
        public void ReturnSettings()
        {
            OnReturnSettings();
            TestUtils.ReturnSettings();
        }

        protected virtual void OnReturnSettings()
        {
        }

        [TearDown]
        public void CleanupAfterTest()
        {
            SelectionController.DeselectAll();
            BeforeCleanup();
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();
            CleanupUtils.CleanupObjects();
            AfterCleanup();
        }

        protected virtual void BeforeCleanup()
        {
        }

        protected virtual void AfterCleanup()
        {
        }
    }
}