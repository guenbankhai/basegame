#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;

namespace BaseGame.Playground.Logging
{
    /// <summary>
    /// Writes every log of a Play session to Logs/Playground.log, replacing the previous session's file. Starts by
    /// itself: no scene, component or reference needed.
    /// </summary>
    internal static class LogCollector
    {
        private static readonly object Lock = new object();
        private static StreamWriter _writer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Start()
        {
            // Domain reload is off, so statics and subscriptions survive between Play sessions.
            Stop();
            try
            {
                string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "Playground.log"));
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                // AutoFlush keeps the last lines if the Editor crashes.
                _writer = new StreamWriter(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read)) { AutoFlush = true };
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogException(e);
                return;
            }

            Application.logMessageReceivedThreaded += OnLogReceived;
            Application.quitting += Stop;
        }

        private static void Stop()
        {
            Application.logMessageReceivedThreaded -= OnLogReceived;
            Application.quitting -= Stop;
            lock (Lock)
            {
                _writer?.Dispose();
                _writer = null;
            }
        }

        // May run on any thread; must not call Debug.Log, or it logs itself forever.
        private static void OnLogReceived(string message, string stackTrace, LogType type)
        {
            lock (Lock)
            {
                if (_writer == null)
                {
                    return;
                }

                try
                {
                    _writer.WriteLine($"{DateTime.Now:HH:mm:ss.fff} [{type}] {message}");
                    if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
                    {
                        _writer.WriteLine(stackTrace);
                    }
                }
                catch (IOException)
                {
                    // Disk full: logging must not break the game, so stop writing.
                    _writer.Dispose();
                    _writer = null;
                }
            }
        }
    }
}
#endif
