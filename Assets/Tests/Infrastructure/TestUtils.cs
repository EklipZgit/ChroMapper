using System;
using System.Collections;
using Beatmap.Helper;
using Beatmap.Info;
using SimpleJSON;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Tests.Infrastructure
{
    internal class TestUtils
    {
        private static bool mapperInit;
        private static int loadVersion = 3;

        private static IEnumerator InitMapper()
        {
            CMInputCallbackInstaller.TestMode = true;
            Settings.TestMode = true;
            yield return SceneManager.LoadSceneAsync("00_FirstBoot", LoadSceneMode.Single);
            PersistentUI.Instance.EnableTransitions = false;

            // On pipeline this may be run fresh
            if (Settings.TestMode)
            {
                var firstBootMenu = Object.FindAnyObjectByType<FirstBootMenu>();
                firstBootMenu.HandleGenerateMissingFolders(0);
            }

            yield return new WaitUntil(() =>
                SceneManager.GetActiveScene().name.StartsWith("01") && !SceneTransitionManager.IsLoading);
            mapperInit = true;
        }

        public static IEnumerator LoadMap(int version)
        {
            if (version != 2 && version != 3) throw new ArgumentException("Only beatmap version 2 and 3 is available");

            var prevVersion = loadVersion;
            loadVersion = version;

            // check map version, switch if different
            if (SceneManager.GetActiveScene().name.StartsWith("03"))
            {
                if (prevVersion == version) yield break;

                SceneTransitionManager.Instance.LoadScene("01_SongSelectMenu");
                yield return new WaitUntil(() =>
                    SceneManager.GetActiveScene().name.StartsWith("01") && !SceneTransitionManager.IsLoading);
            }

            Settings.TestRunnerSettings.MapVersion = version;

            yield return LoadMapper();
        }

        // Load a fresh test map after a scene transition so transition tests recreate the map-scoped services used by later fixtures.
        public static IEnumerator ReloadMap(
            int version,
            JSONNode difficultyJson,
            JSONObject editorState = null)
        {
            if (version != 2 && version != 3) throw new ArgumentException("Only beatmap version 2 and 3 is available");

            loadVersion = version;
            if (SceneManager.GetActiveScene().name.StartsWith("03"))
            {
                // Match PauseManager's normal non-multiplayer exit path before loading the next selected difficulty.
                SceneTransitionManager.Instance.LoadScene("02_SongEditMenu");
                yield return new WaitUntil(() =>
                    SceneManager.GetActiveScene().name.StartsWith("02") && !SceneTransitionManager.IsLoading);
            }

            Settings.TestRunnerSettings.MapVersion = version;
            yield return LoadMapper(difficultyJson, editorState);
        }

        private static IEnumerator LoadMapper(
            JSONNode difficultyJson = null,
            JSONObject editorState = null)
        {
            if (SceneManager.GetActiveScene().name.StartsWith("03")) yield break;

            if (!mapperInit) yield return InitMapper();

            var info = new BaseInfo { Directory = "testmap", SongName = "test" };
            // Inject map-owned editor metadata before scene loading so providers restore it through the same LoadInitialMap path as production maps.
            if (editorState != null)
            {
                info.CustomEditorsData.SetEditorData("editorState", editorState);
            }
            BeatSaberSongContainer.Instance.Info = info;
            var parentSet = new InfoDifficultySet { Characteristic = "Lawless" };
            var diff = new InfoDifficulty(parentSet);

            BeatSaberSongContainer.Instance.MapDifficultyInfo = diff;
            BeatSaberSongContainer.Instance.LoadedSong = AudioClip.Create("Fake", 44100 * 20, 1, 44100, false);
            BeatSaberSongContainer.Instance.Map = BeatmapFactory.GetDifficultyFromJson(
                difficultyJson ?? (loadVersion == 3
                    ? new JSONObject { ["version"] = "3.2.0" }
                    : new JSONObject { ["_version"] = "2.6.0" }),
                "testmap",
                info,
                diff);
            SceneTransitionManager.Instance.LoadScene("03_Mapper");
            yield return new WaitUntil(() => !SceneTransitionManager.IsLoading);
        }

        public static void ReturnSettings()
        {
            Settings.TestMode = false;
        }
    }
}
