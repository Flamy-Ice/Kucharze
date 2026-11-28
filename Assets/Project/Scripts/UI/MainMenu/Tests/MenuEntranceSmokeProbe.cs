using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace ModularMovement
{
    // Checks the saved menu entrance in a development build only when explicitly requested.
    public sealed class MenuEntranceSmokeProbe : MonoBehaviour
    {
        private string report;
        private MenuPanelEntrance entrance;
        private bool startedHidden;
        private float originalTimeScale;
        private IEnumerator scenario;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (!Debug.isDebugBuild || Application.isEditor) return;
            string[] arguments = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(arguments, "-kucharze-menu-entrance-probe");
            if (index < 0 || index + 1 >= arguments.Length) return;
            var probe = new GameObject("MenuEntranceSmokeProbe").AddComponent<MenuEntranceSmokeProbe>();
            probe.report = arguments[index + 1];
            probe.entrance = FindAnyObjectByType<MenuPanelEntrance>();
            probe.startedHidden = probe.entrance != null && probe.entrance.IsPlaying
                && probe.entrance.GetComponent<CanvasGroup>().alpha < 0.01f;
            probe.originalTimeScale = Time.timeScale;
            probe.scenario = probe.Run();
            Application.runInBackground = true;
        }

        private void Update()
        {
            try { if (scenario != null && !scenario.MoveNext()) Finish("PASS", 0); }
            catch (Exception exception) { Finish("FAIL: " + exception, 1); }
        }

        private IEnumerator Run()
        {
            Check(startedHidden, "The home panel flashed fully visible before its entrance.");
            var rect = entrance.GetComponent<RectTransform>();
            var group = entrance.GetComponent<CanvasGroup>();
            var navigation = FindAnyObjectByType<MainMenuNavigation>();
            Check(navigation != null, "The menu navigation is missing.");
            entrance.Advance(2f);
            entrance.Advance(2f);
            Check(entrance.IsPlaying && group.alpha < 0.8f, "A long startup frame skipped the entire entrance.");
            int visibleStartupFrames = 0;
            float until = Time.realtimeSinceStartup + 1.2f;
            while (Time.realtimeSinceStartup < until)
            {
                if (group.alpha > 0 && group.alpha < 1) visibleStartupFrames++;
                yield return null;
            }
            Check(visibleStartupFrames >= 3, "Startup did not show multiple frames of the entrance.");
            CheckResting(rect, group);

            Time.timeScale = 0;
            navigation.ShowOptions();
            navigation.ShowHome();
            Check(entrance.IsPlaying && group.alpha == 0, "Returning home did not replay the entrance.");
            until = Time.realtimeSinceStartup + 0.2f;
            while (Time.realtimeSinceStartup < until) yield return null;
            Check(group.alpha > 0.1f, "The entrance did not advance with the game clock stopped.");
            navigation.ShowPlay();
            Check(!entrance.gameObject.activeSelf, "Play navigation did not leave Home.");
            CheckResting(rect, group);
            navigation.ShowHome();
            Check(group.alpha == 0 && entrance.IsPlaying, "An interrupted entrance did not restart cleanly.");
            until = Time.realtimeSinceStartup + 0.8f;
            while (Time.realtimeSinceStartup < until) yield return null;
            CheckResting(rect, group);

            foreach (int screen in new[] { 1, 2, 3, 0 })
            {
                navigation.Show(screen);
                var active = FindAnyObjectByType<MenuPanelEntrance>();
                Check(active != null && active.IsPlaying && active.GetComponent<CanvasGroup>().alpha == 0,
                    "A menu screen has no entrance: " + screen);
                for (int step = 0; step < 90; step++) active.Advance(1f / 60f);
                CheckResting(active.GetComponent<RectTransform>(), active.GetComponent<CanvasGroup>());
            }
        }

        private void CheckResting(RectTransform rect, CanvasGroup group)
        {
            Check(!rect.GetComponent<MenuPanelEntrance>().IsPlaying && group.alpha == 1 && group.interactable && group.blocksRaycasts,
                "The entrance left the panel hidden or blocked.");
            Check(rect.anchoredPosition.sqrMagnitude < 0.00001f && (rect.localScale - Vector3.one).sqrMagnitude < 0.00001f,
                "The entrance changed the authored resting layout.");
            Check(Quaternion.Angle(rect.localRotation, Quaternion.identity) < 0.001f,
                "The entrance left the menu card rotated.");
        }

        private static void Check(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); }

        private void Finish(string result, int code)
        {
            scenario = null;
            Time.timeScale = originalTimeScale;
            File.WriteAllText(report, result);
            enabled = false;
            Application.Quit(code);
        }
    }
}
