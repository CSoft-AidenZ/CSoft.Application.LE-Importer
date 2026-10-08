using System;
using System.IO;

namespace LE_Importer
{
    public enum LogLevel
    {
        Info,
        Warning,
        Error,
        Success
    }

    /// <summary>
    /// Thread-safe logger that creates a 'Log' folder and generates daily log files based on the current date.
    /// </summary>
    public class Logger : IDisposable
    {
        private static readonly object _lock = new object();

        // 1. Explicitly set directory to a folder named "Log"
        private static readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");

        private readonly string _functionName;
        private readonly DateTime _startTime;

        /// <summary>
        /// Constructor for tracking function scopes with 'using'.
        /// </summary>
        public Logger(string functionName)
        {
            _functionName = functionName;
            _startTime = DateTime.Now;
            Log(LogLevel.Info, $"[START] Execution started for '{_functionName}'");
        }

        /// <summary>
        /// Automatically called when exiting a 'using' block to log total execution duration.
        /// </summary>
        public void Dispose()
        {
            TimeSpan duration = DateTime.Now - _startTime;
            Log(LogLevel.Info, $"[END] Execution completed for '{_functionName}' in {duration.TotalMilliseconds:F0} ms");
        }

        /// <summary>
        /// Writes a log message to the date-based file inside the Log folder.
        /// </summary>
        public static void Log(LogLevel level, string message)
        {
            try
            {
                lock (_lock)
                {
                    // Ensure the 'Log' directory exists
                    if (!Directory.Exists(LogDirectory))
                    {
                        Directory.CreateDirectory(LogDirectory);
                    }

                    // 2. Generate file name based on current date (e.g., Log_2026-07-27.txt)
                    string logFileName = $"Log_{DateTime.Now:yyyy-MM-dd}.txt";
                    string logFilePath = Path.Combine(LogDirectory, logFileName);

                    string formattedEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level.ToString().ToUpper()}] {message}";

                    // Append entry to file
                    File.AppendAllText(logFilePath, formattedEntry + Environment.NewLine);

                    // Also output to Visual Studio Debug Output Window
                    System.Diagnostics.Debug.WriteLine(formattedEntry);
                }
            }
            catch
            {
                // Prevent logging exceptions from throwing or crashing the main application
            }
        }

        // Shortcut Methods
        public static void Info(string message) => Log(LogLevel.Info, message);
        public static void Success(string message) => Log(LogLevel.Success, message);
        public static void Warning(string message) => Log(LogLevel.Warning, message);
        public static void Error(string message, Exception ex = null)
        {
            string fullMsg = ex != null ? $"{message} | Exception: {ex.Message}\n{ex.StackTrace}" : message;
            Log(LogLevel.Error, fullMsg);
        }
    }
}