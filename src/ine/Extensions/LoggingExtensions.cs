using ine.Domain;
using System;

namespace ine.Extensions
{
    public static class LoggingExtensions
    {
        public static void Fatal(this Action<LogEntry> handler, string message, params object[] parameters)
        {
            Log(handler, "Fatal", message, null, null, parameters);
        }

        public static void Error(this Action<LogEntry> handler, string message, params object[] parameters)
        {
            Log(handler, "ERROR", message, null, null, parameters);
        }

        public static void Warning(this Action<LogEntry> handler, string message, params object[] parameters)
        {
            Log(handler, "WARN", message, null, null, parameters);
        }

        public static void Information(this Action<LogEntry> handler, string message, params object[] parameters)
        {
            Log(handler, "INFO", message, null, null, parameters);
        }

        public static void Debug(this Action<LogEntry> handler, string message, params object[] parameters)
        {
            Log(handler, "DEBUG", message, null, null, parameters);
        }

        public static void Debug(this Action<LogEntry> handler, string message, byte[] data, string name, params object[] parameters)
        {
            Log(handler, "DEBUG", message, data, name, parameters);
        }

        private static void Log(Action<LogEntry> handler, string level, string message, byte[] data, string type, object[] parameters)
        {
            if (handler != null)
            {
                LogAttachment attachment = null;

                if (data != null && type != null)
                {
                    attachment = new LogAttachment
                    {
                        Type = type,
                        Data = data
                    };
                }

                handler.Invoke(new LogEntry
                {
                    Level = level,
                    Message = String.Format(message, parameters),
                    Attachment = attachment
                });
            }
        }
    }
}
