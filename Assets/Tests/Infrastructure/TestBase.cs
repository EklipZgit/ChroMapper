using System.Collections;
using Beatmap.Enums;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Infrastructure
{
    public abstract class TestBase
    {
        protected virtual EditingMode InitialEditingMode => EditingMode.Gameplay;

        [UnityOneTimeSetUp]
        public IEnumerator LoadMap()
        {
            yield return TestUtils.LoadMap(3);
            yield return OnMapLoaded();
        }

        [SetUp]
        public void SetUpEditorMode()
        {
            // Establish a deterministic tab before each test so editor mode cannot leak from a preceding fixture.
            var editModeContext = Object.FindAnyObjectByType<EditModeContext>();
            if (editModeContext != null)
                editModeContext.EditingMode = InitialEditingMode;
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

            // Leave the shared editor in the default tab so tests that do not override their mode start consistently.
            //  Without this, test execution order can change the results of tests who forget to properly set their editor tab (CompositeTest, looking at you :V)
            var editModeContext = Object.FindAnyObjectByType<EditModeContext>();
            if (editModeContext != null)
                editModeContext.EditingMode = EditingMode.Gameplay;
        }

        protected virtual void BeforeCleanup()
        {
        }

        protected virtual void AfterCleanup()
        {
        }
    }
}
