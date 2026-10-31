using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace BaseGame.Playground.Logging
{
    /// <summary>Manual test panel: one button per kind of log the collector must catch.</summary>
    [RequireComponent(typeof(LogCollector))]
    public sealed class LogTestOverlay : MonoBehaviour
    {
        private LogCollector _collector;
        private bool _throwInUpdate;

        private void Awake() => _collector = GetComponent<LogCollector>();

        private void Update()
        {
            if (_throwInUpdate)
            {
                _throwInUpdate = false;
                throw new InvalidOperationException($"{nameof(LogTestOverlay)}: uncaught test exception.");
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10f, 10f, 620f, Screen.height - 20f), GUI.skin.box);
            GUILayout.Label($"File: {_collector.FilePath ?? "(collector failed, see Console)"}");
            GUILayout.Label($"Filter: {Debug.unityLogger.filterLogType}");

            if (GUILayout.Button("Log"))
            {
                Debug.Log($"{nameof(LogTestOverlay)}: test log.", this);
            }

            if (GUILayout.Button("LogWarning"))
            {
                Debug.LogWarning($"{nameof(LogTestOverlay)}: test warning.", this);
            }

            if (GUILayout.Button("LogError"))
            {
                Debug.LogError($"{nameof(LogTestOverlay)}: test error.", this);
            }

            if (GUILayout.Button("LogException"))
            {
                Debug.LogException(new InvalidOperationException($"{nameof(LogTestOverlay)}: handled test exception."), this);
            }

            if (GUILayout.Button("Uncaught exception"))
            {
                _throwInUpdate = true;
            }

            if (GUILayout.Button("Log from a worker thread"))
            {
                Task.Run(() => Debug.Log($"{nameof(LogTestOverlay)}: test log from a worker thread."));
            }

            if (GUILayout.Button("Filter: Log"))
            {
                Debug.unityLogger.filterLogType = LogType.Log;
            }

            if (GUILayout.Button("Filter: Warning"))
            {
                Debug.unityLogger.filterLogType = LogType.Warning;
            }

            if (_collector.FilePath != null && GUILayout.Button("Open log folder"))
            {
                Application.OpenURL("file://" + Path.GetDirectoryName(_collector.FilePath));
            }

            GUILayout.EndArea();
        }
    }
}
