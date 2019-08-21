using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LoggerFunc = System.Action<object>;
namespace SynthesisMultiplayer.Util
{
    public class Logger
    {
        public enum LogLevel
        {
            Info,
            Debug,
            Warning,
            Error,
            Fatal
        }
        private static void skip() { }
        Dictionary<LogLevel, LoggerFunc> Loggers;
        public static Logger Instance { get { return Internal.instance; } }
        private Logger()
        {
            Loggers = new Dictionary<LogLevel, LoggerFunc>();
            Loggers[LogLevel.Info] = (o) =>
                Console.WriteLine($"[info] {o}");
            Loggers[LogLevel.Debug] = (o) =>
                Console.WriteLine($"[debug] {o}");
            Loggers[LogLevel.Warning] = (o) =>
                Console.WriteLine($"[warning] {o}");
            Loggers[LogLevel.Error] = (o) =>
                Console.WriteLine($"[error] {o}");
        }
        private class Internal
        {
            static Internal() { }
            public static readonly Logger instance = new Logger();
        }
        public static void RegisterLogger(LogLevel level, LoggerFunc logger) =>
            Instance.Loggers[level] = logger;
        protected static void Log(LogLevel level, object message)
        {
            try
            {
                Instance.Loggers[level](message);
            }
            catch (Exception)
            {
                throw new Exception($"Failed to write to logger '{Enum.GetName(typeof(LogLevel), level)}'");
            }
        }
        public static void LogInfo(object message) => Log(LogLevel.Info, message);
        public static void LogDebug(object message) =>
#if DEBUG || DEBUG_LOGGING
            Log(LogLevel.Info, message);
#else
            skip();
#endif
        public static void LogWarning(object message) => Log(LogLevel.Info, message);
        public static void LogError(object message) => Log(LogLevel.Info, message);
    }
}
