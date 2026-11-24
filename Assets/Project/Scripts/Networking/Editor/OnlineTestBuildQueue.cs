#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Compilation;

// Waits for imported scripts to compile and reload before starting an online test build.
[InitializeOnLoad]
public static class OnlineTestBuildQueue
{
    private const string StageKey = "Kucharze.OnlineTestBuild.Stage";
    private const string ReportKey = "Kucharze.OnlineTestBuild.Report";
    private const string ErrorKey = "Kucharze.OnlineTestBuild.Error";
    private enum Stage { None, Refresh, Compiling, Reload, Ready, Building, Linking }
    private static double readyAt;

    private static Stage Current
    {
        get => (Stage)SessionState.GetInt(StageKey, 0);
        set => SessionState.SetInt(StageKey, (int)value);
    }

    static OnlineTestBuildQueue()
    {
        CompilationPipeline.compilationStarted += OnCompilationStarted;
        CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompiled;
        CompilationPipeline.compilationFinished += OnCompilationFinished;
        AssemblyReloadEvents.afterAssemblyReload += OnReloaded;
        EditorApplication.update += Tick;
        // SessionState survives the reload that replaces the editor's old build callbacks.
        if (Current == Stage.Compiling || Current == Stage.Reload)
            EditorApplication.delayCall += OnReloaded;
    }

    public static void Request(string reportPath)
    {
        if (Current != Stage.None || BuildPipeline.isBuildingPlayer)
            throw new InvalidOperationException("An online test build is already queued or running.");
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before building.");
        if (!File.Exists(OnlineTestSetup.ScenePath))
            throw new FileNotFoundException("Prepare the online test scene first.");
        SessionState.SetString(ReportKey, reportPath ?? "");
        SessionState.SetString(ErrorKey, "");
        Current = Stage.Refresh;
        WriteReport("QUEUED: waiting for script compilation and assembly reload.");
    }

    private static void Tick()
    {
        if (Current == Stage.None || Current == Stage.Building || EditorApplication.isCompiling
            || EditorApplication.isUpdating || BuildPipeline.isBuildingPlayer) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (Current == Stage.Refresh)
        {
            Current = Stage.Compiling;
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            CompilationPipeline.RequestScriptCompilation();
            return;
        }
        if (Current != Stage.Ready || EditorApplication.timeSinceStartup < readyAt) return;
        if (!OnlineServicesEditorStatus.IsReady)
        {
            Current = Stage.Linking;
            _ = LinkThenResumeAsync();
            return;
        }
        try
        {
            if (EditorUtility.scriptCompilationFailed)
                throw new InvalidOperationException("Fix the script compilation errors before building.");
            Current = Stage.Building;
            string output = Path.Combine(Path.GetTempPath(), "KucharzeOnlineTest", "KucharzeOnlineTest.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { OnlineTestSetup.ScenePath }, locationPathName = output,
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development
            });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new InvalidOperationException("The Windows test build failed: " + report.summary.result);
            WriteReport("SUCCESS: build after script compilation and assembly reload.\n" + output);
        }
        catch (Exception exception) { WriteReport("FAILED: " + exception.Message); }
        finally { Current = Stage.None; }
    }

    private static async Task LinkThenResumeAsync()
    {
        await OnlineServicesProjectLink.LinkConfiguredProjectAsync(SessionState.GetString(ReportKey, ""));
        if (!OnlineServicesEditorStatus.IsReady)
        {
            Current = Stage.None;
            return;
        }
        Current = Stage.Ready;
        readyAt = EditorApplication.timeSinceStartup + 1d;
    }

    private static void OnCompilationStarted(object context)
    {
        if (Current == Stage.None || Current == Stage.Building) return;
        SessionState.SetString(ErrorKey, "");
        Current = Stage.Compiling;
    }

    private static void OnAssemblyCompiled(string assembly, CompilerMessage[] messages)
    {
        if (Current != Stage.Compiling) return;
        CompilerMessage error = messages.FirstOrDefault(message => message.type == CompilerMessageType.Error);
        if (error.type == CompilerMessageType.Error && !string.IsNullOrEmpty(error.message))
            SessionState.SetString(ErrorKey, error.message);
    }

    private static void OnCompilationFinished(object context)
    {
        if (Current != Stage.Compiling) return;
        string error = SessionState.GetString(ErrorKey, "");
        if (error.Length > 0)
        {
            WriteReport("FAILED: script compilation. " + error);
            Current = Stage.None;
        }
        else Current = Stage.Reload;
    }

    private static void OnReloaded()
    {
        if (Current != Stage.Compiling && Current != Stage.Reload) return;
        Current = Stage.Ready;
        readyAt = EditorApplication.timeSinceStartup + 1d;
        WriteReport("READY: scripts compiled and editor assemblies reloaded.");
    }

    private static void WriteReport(string message)
    {
        string path = SessionState.GetString(ReportKey, "");
        if (path.Length > 0) File.WriteAllText(path, message);
    }
}
#endif
