using System.Linq;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tests.Editor
{
    public class EditModePlayingCameraTest : TestBase
    {
        // SwitchingToGlsWhilePlayingKeepsTheGameplayWorkspace reproduces issue 51a19: F2 must not disable the
        // gameplay tracks that drive the playing camera while the editor is in a preview UI mode.
        [Test]
        public void SwitchingToGlsWhilePlayingKeepsTheGameplayWorkspace()
        {
            var uiMode = Object.FindAnyObjectByType<UIMode>();
            var editMode = Object.FindAnyObjectByType<EditModeContext>();
            var inputFixture = new InputTestFixture();
            InputAction glsShortcut = null;

            try
            {
                uiMode.SetUIMode(UIModeType.Playing, false);
                editMode.EditingMode = EditingMode.Gameplay;

                inputFixture.Setup();
                var keyboard = InputSystem.AddDevice<Keyboard>();
                glsShortcut = new InputAction(binding: "<Keyboard>/f2");
                glsShortcut.performed += editMode.OnGLSEdit;
                glsShortcut.Enable();

                inputFixture.Press(keyboard.f2Key);

                Assert.That(
                    editMode.EditingMode,
                    Is.EqualTo(EditingMode.Gameplay),
                    "F2 changed the editing workspace while Playing mode still depended on gameplay camera tracks.");
            }
            finally
            {
                glsShortcut?.Dispose();
                inputFixture.TearDown();
                uiMode.SetUIMode(UIModeType.Normal, false);
                editMode.EditingMode = EditingMode.Gameplay;
            }
        }

        // SwitchingToBasicEventsWhilePlayingKeepsTheGameplayWorkspace checks the unguarded F3 sibling of issue 51a19.
        [Test]
        public void SwitchingToBasicEventsWhilePlayingKeepsTheGameplayWorkspace()
        {
            var uiMode = Object.FindAnyObjectByType<UIMode>();
            var editMode = Object.FindAnyObjectByType<EditModeContext>();
            var inputFixture = new InputTestFixture();
            InputAction basicEventShortcut = null;

            try
            {
                uiMode.SetUIMode(UIModeType.Playing, false);
                editMode.EditingMode = EditingMode.Gameplay;

                inputFixture.Setup();
                var keyboard = InputSystem.AddDevice<Keyboard>();
                basicEventShortcut = new InputAction(binding: "<Keyboard>/f3");
                basicEventShortcut.performed += editMode.OnBasicEventEdit;
                basicEventShortcut.Enable();

                inputFixture.Press(keyboard.f3Key);

                Assert.That(
                    editMode.EditingMode,
                    Is.EqualTo(EditingMode.Gameplay),
                    "F3 changed the editing workspace while Playing mode still depended on gameplay camera tracks.");
            }
            finally
            {
                basicEventShortcut?.Dispose();
                inputFixture.TearDown();
                uiMode.SetUIMode(UIModeType.Normal, false);
                editMode.EditingMode = EditingMode.Gameplay;
            }
        }

        // EnteringPlayingFromAnotherWorkspaceKeepsGameplayCameraTracksActive checks the alternate issue 51a19
        // sequence where the workspace changes before the transient playing camera is selected.
        [TestCase(EditingMode.GLS)]
        [TestCase(EditingMode.BasicEvent)]
        public void EnteringPlayingFromAnotherWorkspaceKeepsGameplayCameraTracksActive(EditingMode initialMode)
        {
            var uiMode = Object.FindAnyObjectByType<UIMode>();
            var editMode = Object.FindAnyObjectByType<EditModeContext>();
            var cameraManager = Object.FindAnyObjectByType<CameraManager>();
            var gameplayTracks = FindGameplayTracks();
            var inputFixture = new InputTestFixture();
            InputAction playingShortcut = null;

            try
            {
                uiMode.SetUIMode(UIModeType.Normal, false);
                editMode.EditingMode = initialMode;

                inputFixture.Setup();
                var keyboard = InputSystem.AddDevice<Keyboard>();
                playingShortcut = new InputAction(binding: "<Keyboard>/5");
                playingShortcut.performed +=
                    ((CMInput.IUIModeActions)uiMode).OnToggleUIModePlaying;
                playingShortcut.Enable();

                inputFixture.Press(keyboard.digit5Key);

                Assert.That(UIMode.SelectedMode, Is.EqualTo(UIModeType.Playing));
                Assert.That(
                    cameraManager.SelectedCameraController,
                    Is.SameAs(cameraManager.CameraControllers[1]),
                    "Playing mode did not select the transient playing camera.");
                Assert.That(
                    gameplayTracks.activeInHierarchy,
                    Is.True,
                    $"Entering Playing from {initialMode} left its camera-driving gameplay tracks inactive.");
                uiMode.SetUIMode(UIModeType.Normal, false);
                Assert.That(
                    editMode.EditingMode,
                    Is.EqualTo(initialMode),
                    "Leaving Playing did not restore the editing workspace that was active before playback preview.");
            }
            finally
            {
                playingShortcut?.Dispose();
                inputFixture.TearDown();
                cameraManager.SelectCamera(CameraType.Editing);
                uiMode.SetUIMode(UIModeType.Normal, false);
                editMode.EditingMode = EditingMode.Gameplay;
            }
        }

        // EscapingPlayingBeforePauseRestoresEditingCamera checks that mode 5 exits to its prior mode and editing
        // camera before a fresh second Escape opens pause.
        [Test]
        public void EscapingPlayingBeforePauseRestoresEditingCamera()
        {
            var uiMode = Object.FindAnyObjectByType<UIMode>();
            var pauseManager = Object.FindAnyObjectByType<PauseManager>();
            var cameraManager = Object.FindAnyObjectByType<CameraManager>();
            var inputFixture = new InputTestFixture();
            InputAction playingAction = null;
            InputAction escapeAction = null;

            try
            {
                inputFixture.Setup();
                var keyboard = InputSystem.AddDevice<Keyboard>();
                playingAction = new InputAction(binding: "<Keyboard>/5");
                playingAction.performed +=
                    ((CMInput.IUIModeActions)uiMode).OnToggleUIModePlaying;
                escapeAction = new InputAction(binding: "<Keyboard>/escape");
                escapeAction.performed += pauseManager.OnPauseEditor;
                playingAction.Enable();
                escapeAction.Enable();

                inputFixture.Press(keyboard.digit5Key);
                Assert.That(cameraManager.SelectedCameraController, Is.SameAs(cameraManager.CameraControllers[1]));
                inputFixture.Press(keyboard.escapeKey);

                Assert.That(PauseManager.IsPaused, Is.False, "The first Escape opened pause instead of exiting Playing.");
                Assert.That(UIMode.SelectedMode, Is.EqualTo(UIModeType.Normal));
                Assert.That(
                    cameraManager.SelectedCameraController,
                    Is.SameAs(cameraManager.CameraControllers[0]),
                    "Exiting Playing restored the Normal enum without restoring its editing camera.");

                inputFixture.Release(keyboard.escapeKey);
                inputFixture.Press(keyboard.escapeKey);
                Assert.That(PauseManager.IsPaused, Is.True, "A fresh second Escape did not open pause normally.");
            }
            finally
            {
                playingAction?.Dispose();
                escapeAction?.Dispose();
                if (PauseManager.IsPaused)
                    pauseManager.TogglePause();
                inputFixture.TearDown();
                cameraManager.SelectCamera(CameraType.Editing);
                uiMode.SetUIMode(UIModeType.Normal, false);
            }
        }

        // SwitchingPreviewModesKeepsOriginalEditorReturnMode ensures Preview and Playing share one editor-mode
        // return target instead of treating either transient mode as the other's previous mode.
        [TestCase(UIModeType.Playing, UIModeType.Preview)]
        [TestCase(UIModeType.Preview, UIModeType.Playing)]
        public void SwitchingPreviewModesKeepsOriginalEditorReturnMode(
            UIModeType firstPreviewMode,
            UIModeType secondPreviewMode)
        {
            var uiMode = Object.FindAnyObjectByType<UIMode>();
            var pauseManager = Object.FindAnyObjectByType<PauseManager>();
            var cameraManager = Object.FindAnyObjectByType<CameraManager>();
            var inputFixture = new InputTestFixture();
            InputAction playingAction = null;
            InputAction previewAction = null;
            InputAction escapeAction = null;

            try
            {
                uiMode.SetUIMode(UIModeType.HideGrids, false);
                inputFixture.Setup();
                var keyboard = InputSystem.AddDevice<Keyboard>();
                playingAction = new InputAction(binding: "<Keyboard>/5");
                playingAction.performed +=
                    ((CMInput.IUIModeActions)uiMode).OnToggleUIModePlaying;
                previewAction = new InputAction(binding: "<Keyboard>/4");
                previewAction.performed +=
                    ((CMInput.IUIModeActions)uiMode).OnToggleUIModePreview;
                escapeAction = new InputAction(binding: "<Keyboard>/escape");
                escapeAction.performed += pauseManager.OnPauseEditor;
                playingAction.Enable();
                previewAction.Enable();
                escapeAction.Enable();

                var firstKey = firstPreviewMode == UIModeType.Playing
                    ? keyboard.digit5Key
                    : keyboard.digit4Key;
                var secondKey = secondPreviewMode == UIModeType.Playing
                    ? keyboard.digit5Key
                    : keyboard.digit4Key;
                inputFixture.Press(firstKey);
                inputFixture.Release(firstKey);
                inputFixture.Press(secondKey);
                inputFixture.Press(keyboard.escapeKey);

                Assert.That(UIMode.SelectedMode, Is.EqualTo(UIModeType.HideGrids));
                Assert.That(PauseManager.IsPaused, Is.False, "Escape opened pause instead of restoring Hide Grids.");
                Assert.That(
                    cameraManager.SelectedCameraController,
                    Is.SameAs(cameraManager.CameraControllers[0]),
                    "Escape did not restore the editing camera after switching transient preview modes.");
            }
            finally
            {
                playingAction?.Dispose();
                previewAction?.Dispose();
                escapeAction?.Dispose();
                if (PauseManager.IsPaused)
                    pauseManager.TogglePause();
                inputFixture.TearDown();
                cameraManager.SelectCamera(CameraType.Editing);
                uiMode.SetUIMode(UIModeType.Normal, false);
            }
        }

        // Camera softlock tests inspect the authoritative scene object even after a workspace deactivates it.
        private static GameObject FindGameplayTracks() => Object
            .FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(transform => transform.name == "Gameplay Container Tracks")
            .gameObject;
    }
}
