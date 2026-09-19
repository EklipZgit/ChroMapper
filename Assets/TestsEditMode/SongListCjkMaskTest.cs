using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TestsEditMode
{
    // Live diagnostics isolate the player-only CJK regression to the song list's stencil-mask
    // presentation path, so inspect the real scene instead of accepting font resolution alone.
    public class SongListCjkMaskTest
    {
        private Scene scene;
        private GameObject viewport;

        // Preview scenes preserve the user's current editor state while exposing the imported UI components.
        [OneTimeSetUp]
        public void OpenSongSelectPreview()
        {
            scene = EditorSceneManager.OpenPreviewScene("Assets/__Scenes/01_SongSelectMenu.unity");
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var candidate in root.GetComponentsInChildren<RectTransform>(true))
                {
                    if (candidate.name == "Viewport" && HasAncestorNamed(candidate, "SongList"))
                    {
                        viewport = candidate.gameObject;
                        break;
                    }
                }

                if (viewport != null)
                {
                    break;
                }
            }

            Assert.That(viewport, Is.Not.Null, "SongList viewport is missing from the song-select scene.");
        }

        // Closing the preview prevents test inspection from changing or saving the production scene.
        [OneTimeTearDown]
        public void CloseSongSelectPreview()
        {
            if (scene.IsValid())
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        // SongListElementPresentsCjkPixelsInsideMask covers rendering; this guards the real scene
        // against restoring the stencil Mask that blanked otherwise healthy TMP fallback submeshes.
        [Test]
        public void SongListViewportUsesRectMask2D()
        {
            Assert.That(viewport.GetComponent<Mask>(), Is.Null,
                "SongList viewport must not create stencil materials for CJK fallback submeshes.");
            Assert.That(viewport.GetComponent<RectMask2D>(), Is.Not.Null,
                "SongList viewport must retain rectangular clipping with RectMask2D.");
        }

        // Direct hierarchy traversal keeps the assertion tied to the SongList when other viewports exist.
        private static bool HasAncestorNamed(Transform child, string name)
        {
            for (var current = child.parent; current != null; current = current.parent)
            {
                if (current.name == name)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
