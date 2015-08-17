using ine.Domain;
using System;

namespace ine.Extensions
{
    public static class LoggingExtensions
    {
        public static void Fatal(this Action<LogEntry> handler, string message, params object[] parameters)
        {
            Log(handler, "Fatal", message, parameters);
        }

        public static void Error(this Action<LogEntry> handler, string message, params object[] parameters)
        {
            Log(handler, "ERROR", message, parameters);
        }

        public static void Warning(this Action<LogEntry> handler, string message, params object[] parameters)
        {
            Log(handler, "WARN", message, parameters);
        }

        public static void Information(this Action<LogEntry> handler, string message, params object[] parameters)
        {
            Log(handler, "INFO", message, parameters);
        }

        public static void Debug(this Action<LogEntry> handler, string message, params object[] parameters)
        {
            Log(handler, "DEBUG", message, parameters);
        }

        private static void Log(Action<LogEntry> handler, string level, string message, object[] parameters)
        {
            if (handler != null)
            {
                handler.Invoke(new LogEntry
                {
                    Level = level,
                    Message = String.Format(message, parameters)
                });
            }
        }
    }
}
