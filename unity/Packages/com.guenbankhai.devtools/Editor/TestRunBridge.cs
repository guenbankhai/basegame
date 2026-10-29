using System;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace GuenBanKhai.DevTools.Editor
{
    /// <summary>
    /// Lets tools outside Unity run EditMode tests in the open Editor through files, and saves every finished run,
    /// including one started by hand. Paths are relative to the project folder; the protocol is in the package README.
    /// </summary>
    [InitializeOnLoad]
    internal sealed class TestRunBridge : ICallbacks
    {
        private const string RequestPath = "Temp/DevTools/run-tests.request";
        private const string StatusPath = "Temp/DevTools/run-tests.status";
        private const string ResultsFolder = "Logs/TestResults";
        private const string CompileErrorsPath = ResultsFolder + "/compile-errors.txt";
        private const string Refreshing = "refreshing";
        private const string Running = "running";
        private const double PollSeconds = 1.0;

        private double _nextPoll;

        static TestRunBridge()
        {
            var bridge = new TestRunBridge();
            TestRunnerApi.RegisterTestCallback(bridge);
            EditorApplication.update += bridge.Poll;
            CompilationPipeline.compilationStarted += _ => DeleteIfExists(CompileErrorsPath);
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompiled;
        }

        public void RunStarted(ITestAdaptor testsToRun)
        {
        }

        public void TestStarted(ITestAdaptor test)
        {
        }

        public void TestFinished(ITestResultAdaptor result)
        {
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            Directory.CreateDirectory(ResultsFolder);
            TestRunnerApi.SaveResultToFile(result, $"{ResultsFolder}/{result.Test.TestMode}.xml");
            if (ReadStatus() == Running)
            {
                WriteStatus($"done passed={result.PassCount} failed={result.FailCount} skipped={result.SkipCount}");
            }
        }

        private void Poll()
        {
            if (EditorApplication.timeSinceStartup < _nextPoll)
            {
                return;
            }

            _nextPoll = EditorApplication.timeSinceStartup + PollSeconds;
            if (!File.Exists(RequestPath) || EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            // Refresh first so the run sees the latest code. A domain reload may follow, so the step lives on disk.
            if (ReadStatus() != Refreshing)
            {
                WriteStatus(Refreshing);
                AssetDatabase.Refresh();
                return;
            }

            string[] assemblyNames = File.ReadAllLines(RequestPath);
            File.Delete(RequestPath);
            if (EditorUtility.scriptCompilationFailed)
            {
                WriteStatus("compile-error");
                return;
            }

            WriteStatus(Running);
            var filter = new Filter
            {
                testMode = TestMode.EditMode,
                assemblyNames = assemblyNames.Length > 0 ? assemblyNames : null,
            };
            ScriptableObject.CreateInstance<TestRunnerApi>().Execute(new ExecutionSettings(filter));
        }

        private static void OnAssemblyCompiled(string assemblyPath, CompilerMessage[] messages)
        {
            foreach (var message in messages)
            {
                if (message.type == CompilerMessageType.Error)
                {
                    Directory.CreateDirectory(ResultsFolder);
                    File.AppendAllText(CompileErrorsPath, $"{message.file}({message.line}): {message.message}{Environment.NewLine}");
                }
            }
        }

        private static string ReadStatus() => File.Exists(StatusPath) ? File.ReadAllText(StatusPath).Trim() : null;

        private static void WriteStatus(string status)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(StatusPath));
            File.WriteAllText(StatusPath, status);
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
