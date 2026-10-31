using System;
using System.IO;
using UnityEngine;

namespace BaseGame.Playground.Logging
{
    /// <summary>
    /// Thin driver: writes every log that passes the filter to a file under persistentDataPath/Logs. Modules do not know
    /// it exists. Runs very early to catch startup logs; put it in a boot scene that loads once.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class LogCollector : MonoBehaviour
    {
        [Tooltip("Sessions to keep, this one included.")]
        [SerializeField] private int _maxFiles = 5;
        [Tooltip("Log keeps everything; Warning drops plain logs. Applies to the whole game while this runs, Console included.")]
        [SerializeField] private LogType _filter = LogType.Log;

        private LogFile _file;
        private LogType _previousFilter;

        public string FilePath => _file?.FilePath;

        private void Awake()
        {
            try
            {
                string directory = Path.Combine(Application.persistentDataPath, "Logs");
                _file = new LogFile(directory, Application.version, _maxFiles, DateTime.UtcNow);
                DontDestroyOnLoad(gameObject);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            {
                Debug.LogException(e, this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            _previousFilter = Debug.unityLogger.filterLogType;
            Debug.unityLogger.filterLogType = _filter;
            Application.logMessageReceivedThreaded += OnLogReceived;
        }

        private void OnDisable()
        {
            Application.logMessageReceivedThreaded -= OnLogReceived;
            // Domain reload is off, so the global filter would otherwise outlive Play mode.
            Debug.unityLogger.filterLogType = _previousFilter;
        }

        private void OnDestroy() => _file?.Dispose();

        private void OnLogReceived(string message, string stackTrace, LogType type) => _file.Write(message, stackTrace, type);
    }
}
