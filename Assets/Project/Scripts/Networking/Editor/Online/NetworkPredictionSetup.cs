#if UNITY_EDITOR && KUCHARZE_ONLINE
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

// Runs the movement regression suite and queues a separate two-process test build.
[InitializeOnLoad]
public static class NetworkPredictionSetup
{
    private const string Request = "Assets/Project/Scripts/Networking/Editor/Online/PredictionSetup.request";
    private const string Result = "Assets/Project/Scripts/Networking/Editor/Online/PredictionSetup.result";
    private static TestRunnerApi runner;

    static NetworkPredictionSetup() => EditorApplication.update += Poll;

    private static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode
            || !File.Exists(Request)) return;
        string operation = File.ReadAllText(Request).Trim();
        if (!AssetDatabase.DeleteAsset(Request)) File.Delete(Request);
        try
        {
            if (operation == "tests") RunTests();
            else if (operation == "build") OnlineTestBuildQueue.Request(Result,
                new[] { OnlineTestSetup.ScenePath }, "KucharzePredictionTest");
            else if (operation == "game") OnlineTestBuildQueue.Request(Result,
                new[] { ProjectAssetPaths.MenuScene, ProjectAssetPaths.LobbyScene, ProjectAssetPaths.GameScene }, "KucharzeUpdated");
            else if (operation == "lobby") OnlineTestBuildQueue.Request(Result,
                new[] { ProjectAssetPaths.LobbyScene, ProjectAssetPaths.GameScene }, "KucharzePredictionLobby");
            else throw new InvalidOperationException("Unknown prediction check.");
        }
        catch (Exception exception) { File.WriteAllText(Result, "FAILED: " + exception); }
    }

    [MenuItem("Tools/Kucharze/Online/Run Movement Replay Tests")]
    public static void RunTests()
    {
        if (runner != null) throw new InvalidOperationException("The movement tests are already running.");
        File.WriteAllText(Result, "RUNNING: movement replay and input boundary tests");
        runner = ScriptableObject.CreateInstance<TestRunnerApi>();
        runner.RegisterCallbacks(new Results());
        runner.Execute(new ExecutionSettings(new Filter
        {
            testMode = TestMode.EditMode, assemblyNames = new[] { "PlayerMovementSystem3D.Tests.EditMode" }
        }));
    }

    private sealed class Results : ICallbacks
    {
        private readonly StringBuilder failures = new StringBuilder();
        public void RunStarted(ITestAdaptor test) { }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result)
        {
            if (result.ResultState.StartsWith("Failed")) failures.AppendLine(result.FullName + ": " + result.Message);
        }
        public void RunFinished(ITestResultAdaptor result)
        {
            File.WriteAllText(Result, (result.FailCount == 0 && result.PassCount > 0 ? "SUCCESS" : "FAILED")
                + ": passed=" + result.PassCount + ", failed=" + result.FailCount + ", skipped=" + result.SkipCount
                + "\n" + failures);
            UnityEngine.Object.DestroyImmediate(runner);
            runner = null;
        }
    }
}
#endif
