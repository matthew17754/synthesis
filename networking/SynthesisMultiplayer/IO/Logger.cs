using Multiplayer.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Multiplayer.IO
{
    public class Info
    {
        public static void Log(object o) =>
            Logger.LogInfo(o);
    }

    public class Debug
    {
        public static void Log(object o) =>
            Logger.LogDebug(o);
    }

    public class Warning
    {
        public static void Log(object o) =>
            Logger.LogWarning(o);
    }

    public class Logger
    {
        public enum LogLevel
        {
            Info,
            Debug,
            Warning,
            Error,
        }
        private static SinkWriter logSink;
        private static void Skip() { }
        Dictionary<LogLevel, TextWriter> Loggers;
        private Mutex LoggerMutex;
        public static Logger Instance { get { return Internal.instance; } }
        private Logger()
        {
            logSink = new SinkWriter();
            LoggerMutex = new Mutex();
            Loggers = new Dictionary<LogLevel, TextWriter>
            {
                [LogLevel.Info] = new LogWriter((o) =>
                    Console.Write(o)),
                [LogLevel.Debug] = new LogWriter((o) =>
                    Console.Write(o)),
                [LogLevel.Warning] = new LogWriter((o) =>
                    Console.Write(o)),
                [LogLevel.Error] = new LogWriter((o) =>
                    Console.Write(o))
            };
        }
        private class Internal
        {
            static Internal() { }
            public static readonly Logger instance = new Logger();
        }
        public static void RegisterLogger(LogLevel level, TextWriter logger)
        {
            Instance.LoggerMutex.WaitOne();
            Instance.Loggers[level] = logger;
            Instance.LoggerMutex.ReleaseMutex();
        }
        protected static void Log(LogLevel level, object message)
        {
            try
            {
                Instance.LoggerMutex.WaitOne();
                Instance.Loggers[level].WriteLine(message);
                Instance.LoggerMutex.ReleaseMutex();
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to write to logger '{Enum.GetName(typeof(LogLevel), level)}'");
            }
        }
        public static void LogInfo(object message) => Log(LogLevel.Info, message);
        public static void LogDebug(object message) =>
#if DEBUG || DEBUG_LOGGING
            Log(LogLevel.Debug, message);
#else
            Skip();
#endif
        public static void LogWarning(object message) => Log(LogLevel.Warning, message);
        public static void LogError(object message) => Log(LogLevel.Error, message);
        public static TextWriter InfoLogger {
            get
            {
                return Instance.Loggers[LogLevel.Info];
            }
        }
        public static TextWriter DebugLogger
        {
            get
            {
#if DEBUG || DEBUG_LOGGING
                return Instance.Loggers[LogLevel.Debug];
#else
                return logSink;
#endif
            }
        }
        public static TextWriter WarningLogger {
            get
            {
                return Instance.Loggers[LogLevel.Warning];
            }
        }
        public static TextWriter ErrorLogger {
            get
            {
                return Instance.Loggers[LogLevel.Error];
            }
        }
    }
}
