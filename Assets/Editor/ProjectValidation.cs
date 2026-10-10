using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class ProjectValidation
{
    internal const string MenuScene = "Assets/Scenes/MainMenu.unity";
    internal const string GameScene = "Assets/Scenes/Ingame.unity";
    private const string Prefix = "SatelliteDefense.Validation.";
    private const string Running = Prefix + "Running";
    private const string DirectoryKey = Prefix + "Directory";
    private const string SaveKey = Prefix + "SaveDirectory";
    private const string ReportKey = Prefix + "Report";
    private static ValidationReport report;

    [Serializable] private class CaseResult
    {
        public string name, outcome, error;
        public int assertions;
        public double seconds;
    }
    [Serializable] private class ValidationReport
    {
        public string startedUtc, outcome = "RUNNING", mode;
        public int assertions, unityErrors;
        public List<CaseResult> cases = new();
    }

    static ProjectValidation()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.update += CheckTimeout;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void IsolateSave()
    {
        if (SessionState.GetBool(Running, false))
            SaveGameController.UseStorageDirectoryForValidation(TestSaveDirectory);
    }

    internal static string TestSaveDirectory => SessionState.GetString(SaveKey, "");
    internal static string RunDirectory => SessionState.GetString(DirectoryKey, "");
    private static ValidationReport Report => report ??= JsonUtility.FromJson<ValidationReport>(SessionState.GetString(ReportKey, "{}"));

    [MenuItem("Tools/Validation/Run all checks")]
    public static void RunFromMenu() => Begin(false, "All");
    [MenuItem("Tools/Validation/Run all checks", true)]
    private static bool CanRun() => !EditorApplication.isPlayingOrWillChangePlaymode && !SessionState.GetBool(Running, false);

    public static void RunBatch() => EditorApplication.delayCall += () => Begin(true, "All");
    public static void RunDataBatch() => EditorApplication.delayCall += () => Begin(true, "Data");
    public static void RunPlayBatch() => EditorApplication.delayCall += () => Begin(true, "Play");

    private static void Begin(bool batch, string mode)
    {
        if (!CanRun()) throw new InvalidOperationException("Validation must start from idle edit mode.");
        string directory = Path.GetFullPath(Path.Combine("Logs", "Validation", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(directory);
        SessionState.SetString(DirectoryKey, directory);
        SessionState.SetString(SaveKey, Path.Combine(directory, "Saves", "initial"));
        SessionState.SetBool(Running, true);
        SessionState.SetBool(Prefix + "Batch", batch);
        SessionState.SetString(Prefix + "PreviousStartScene", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        report = new ValidationReport { mode = mode, startedUtc = DateTime.UtcNow.ToString("O") };
        SaveReport();
        Application.logMessageReceived += CaptureError;
        try
        {
            if (mode != "Play")
            {
                RunCase("Save codec and invalid data", ProjectDataValidation.CheckCodec);
                RunCase("Shop transactions and immutable definitions", ProjectDataValidation.CheckShopRules);
                RunCase("Loadout aggregation and boundaries", ProjectDataValidation.CheckStats);
                RunCase("Catalog, JSON, prefab references and progression", ProjectDataValidation.CheckAssets);
                RunCase("Scene separation and serialized references", ProjectDataValidation.CheckScenes);
                RunCase("Filesystem, recovery, transactions and autosave", () => SaveStorageValidation.Run(Require));
            }
            if (mode == "Data") { Complete(); return; }
            // Scene checks also protect a play-only invocation from invalid scene setup.
            if (mode == "Play") RunCase("Scene setup", ProjectDataValidation.CheckScenes);
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScene);
            if (!scene) throw new InvalidOperationException("MainMenu scene is missing.");
            EditorSceneManager.playModeStartScene = scene;
            SessionState.SetFloat(Prefix + "Deadline", (float)(EditorApplication.timeSinceStartup + 180));
            EditorApplication.EnterPlaymode();
        }
        catch (Exception error)
        {
            RecordCase("Runner setup", error, 0, 0);
            Complete();
        }
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Running, false)) return;
        report = null;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            Application.logMessageReceived -= CaptureError;
            Application.logMessageReceived += CaptureError;
            SessionState.SetFloat(Prefix + "Deadline", (float)(EditorApplication.timeSinceStartup + 600));
            new GameObject("Regression checks").AddComponent<PlayModeValidationRunner>().StartChecks();
        }
        else if (state == PlayModeStateChange.EnteredEditMode) Complete();
    }

    private static void CheckTimeout()
    {
        if (!SessionState.GetBool(Running, false)) return;
        float deadline = SessionState.GetFloat(Prefix + "Deadline", 0);
        if (deadline > 0 && EditorApplication.timeSinceStartup > deadline)
        {
            SessionState.SetFloat(Prefix + "Deadline", 0);
            Finish(new TimeoutException("Validation exceeded its timeout."));
        }
    }

    private static void CaptureError(string message, string stack, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        Report.unityErrors++;
        Log("UNITY ERROR: " + message + "\n" + stack);
        SaveReport();
    }

    internal static void Require(bool condition, string message)
    {
        Report.assertions++;
        if (!condition) throw new InvalidOperationException(message);
    }

    internal static void Near(float actual, float expected, string message, float tolerance = 0.0001f) =>
        Require(!float.IsNaN(actual) && Mathf.Abs(actual - expected) <= tolerance, $"{message}: expected {expected}, got {actual}");

    private static void RunCase(string name, Action check)
    {
        int before = Report.assertions;
        double start = EditorApplication.timeSinceStartup;
        Exception error = null;
        try { check(); } catch (Exception failure) { error = failure; }
        RecordCase(name, error, Report.assertions - before, EditorApplication.timeSinceStartup - start);
    }

    internal static void RecordCase(string name, Exception error, int assertions, double seconds)
    {
        Report.cases.Add(new CaseResult { name = name, outcome = error == null ? "PASS" : "FAIL", error = error?.ToString(), assertions = assertions, seconds = seconds });
        Log((error == null ? "PASS: " : "FAIL: ") + name + (error == null ? "" : "\n" + error));
        SaveReport();
    }

    internal static int AssertionCount => Report.assertions;
    internal static void PrepareCase(string name, int seed)
    {
        string safeName = name.Replace(' ', '-').Replace('/', '-');
        string directory = Path.Combine(RunDirectory, "Saves", safeName);
        SessionState.SetString(SaveKey, directory);
        SaveGameController.UseStorageDirectoryForValidation(directory);
        UnityEngine.Random.InitState(seed);
        Time.timeScale = 1f;
    }

    internal static void Finish(Exception error = null)
    {
        if (error != null) RecordCase("Runner", error, 0, 0);
        SaveReport();
        SessionState.SetFloat(Prefix + "Deadline", 0);
        if (EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.ExitPlaymode();
        else Complete();
    }

    private static void Complete()
    {
        Application.logMessageReceived -= CaptureError;
        try { SaveGameController.CloseStorageForValidation(); }
        catch (Exception error) { RecordCase("Save cleanup", error, 0, 0); }
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString(Prefix + "PreviousStartScene", ""));
        // A manual stop before Finish must never report a successful run.
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        bool finished = Report.mode == "Data" || SessionState.GetFloat(Prefix + "Deadline", 0) == 0;
        bool passed = finished && Report.unityErrors == 0 && Report.cases.Count > 0 && Report.cases.TrueForAll(item => item.outcome == "PASS");
        Report.outcome = passed ? "PASS" : "FAIL";
        if (!finished) Log("FAIL: Play mode was stopped before checks completed.");
        SaveReport();
        Log($"{Report.outcome}: {Report.cases.Count} cases, {Report.assertions} assertions, {Report.unityErrors} Unity errors. Results: {RunDirectory}");
        Debug.Log($"Validation {Report.outcome}. Results: {RunDirectory}");
        File.WriteAllText("Logs/Validation-results.txt", $"{Report.outcome}\n{RunDirectory}\n");
        bool batch = SessionState.GetBool(Prefix + "Batch", false);
        SessionState.SetBool(Running, false);
        SessionState.SetFloat(Prefix + "Deadline", 0);
        SessionState.EraseString(SaveKey);
        // Reports and isolated saves are retained to diagnose failures; no player save is deleted.
        if (batch) EditorApplication.Exit(passed ? 0 : 1);
    }

    private static void SaveReport()
    {
        string json = JsonUtility.ToJson(Report, true);
        SessionState.SetString(ReportKey, json);
        File.WriteAllText(Path.Combine(RunDirectory, "results.json"), json);
    }

    private static void Log(string text) => File.AppendAllText(Path.Combine(RunDirectory, "results.txt"), text + "\n");

    // Kept as a separately invoked build entry point. Regression checks never call it.
    public static void BuildAndroidDevelopment()
    {
        bool useKeystore = PlayerSettings.Android.useCustomKeystore;
        try
        {
            PlayerSettings.Android.useCustomKeystore = false;
            Directory.CreateDirectory("Builds");
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { MenuScene, GameScene }, target = BuildTarget.Android,
                locationPathName = "Builds/SatelliteDefense-development.apk", options = BuildOptions.Development
            });
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/Android-build-result.txt", $"{result.summary.result}: {result.summary.totalErrors} errors, {result.summary.totalWarnings} warnings\n");
            if (result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new InvalidOperationException("Android development build failed.");
        }
        finally { PlayerSettings.Android.useCustomKeystore = useKeystore; }
    }
}

public class PlayModeValidationRunner : MonoBehaviour
{
    public void StartChecks()
    {
        DontDestroyOnLoad(gameObject);
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        int index = 0;
        foreach (var test in ProjectPlayValidation.Cases())
        {
            int before = ProjectValidation.AssertionCount;
            double started = EditorApplication.timeSinceStartup;
            Exception error = null;
            var stack = new Stack<IEnumerator>();
            try
            {
                ProjectValidation.PrepareCase(test.name, 7100 + index++);
                stack.Push(test.check());
            }
            catch (Exception failure) { error = failure; }
            while (error == null && stack.Count > 0)
            {
                object current = null;
                bool next = false;
                try
                {
                    next = stack.Peek().MoveNext();
                    if (next) current = stack.Peek().Current;
                    else (stack.Pop() as IDisposable)?.Dispose();
                }
                catch (Exception failure) { error = failure; }
                if (error != null) break;
                if (!next) continue;
                if (current is IEnumerator nested && !(current is CustomYieldInstruction)) stack.Push(nested);
                else yield return current;
            }
            while (stack.Count > 0)
            {
                try { (stack.Pop() as IDisposable)?.Dispose(); }
                catch (Exception cleanup) { error ??= cleanup; }
            }
            ProjectValidation.RecordCase(test.name, error, ProjectValidation.AssertionCount - before, EditorApplication.timeSinceStartup - started);
        }
        ProjectValidation.Finish();
    }
}
