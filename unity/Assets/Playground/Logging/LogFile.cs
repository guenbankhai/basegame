using System;
using System.IO;
using UnityEngine;

namespace BaseGame.Playground.Logging
{
    /// <summary>
    /// One session's log file; older sessions beyond maxFiles are deleted. Write is safe from any thread and never
    /// calls Debug.Log, because it runs inside the log callback and would log itself forever.
    /// </summary>
    public sealed class LogFile : IDisposable
    {
        private const string FilePrefix = "log-";

        private readonly object _lock = new object();
        private StreamWriter _writer;

        public LogFile(string directory, string version, int maxFiles, DateTime startUtc)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentException("Directory must not be empty.", nameof(directory));
            }

            if (maxFiles < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxFiles), maxFiles, "Must be >= 1.");
            }

            Directory.CreateDirectory(directory);
            FilePath = Path.Combine(directory, $"{FilePrefix}{startUtc:yyyyMMdd-HHmmss-fff}.txt");
            // FileShare.Read lets the file be opened while the game runs; AutoFlush keeps the last lines after a crash.
            _writer = new StreamWriter(new FileStream(FilePath, FileMode.Create, FileAccess.Write, FileShare.Read)) { AutoFlush = true };
            _writer.WriteLine($"Version {version}");
            DeleteOldest(directory, maxFiles);
        }

        public string FilePath { get; }

        public void Write(string message, string stackTrace, LogType type)
        {
            lock (_lock)
            {
                if (_writer == null)
                {
                    return;
                }

                try
                {
                    _writer.WriteLine($"{DateTime.UtcNow:O} [{type}] {message}");
                    bool isFailure = type == LogType.Exception || type == LogType.Error || type == LogType.Assert;
                    if (isFailure && !string.IsNullOrEmpty(stackTrace))
                    {
                        _writer.WriteLine(stackTrace);
                    }
                }
                catch (IOException)
                {
                    // Disk full or file gone: logging must not break the game, so stop writing.
                    _writer.Dispose();
                    _writer = null;
                }
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _writer?.Dispose();
                _writer = null;
            }
        }

        private static void DeleteOldest(string directory, int maxFiles)
        {
            string[] files = Directory.GetFiles(directory, $"{FilePrefix}*.txt");
            // The timestamp in the name sorts oldest first.
            Array.Sort(files, StringComparer.Ordinal);
            for (int i = 0; i < files.Length - maxFiles; i++)
            {
                try
                {
                    File.Delete(files[i]);
                }
                catch (IOException)
                {
                    // Another running instance may hold it; the next session tries again.
                }
            }
        }
    }
}
