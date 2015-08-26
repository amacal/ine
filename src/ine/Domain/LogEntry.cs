namespace ine.Domain
{
    public class LogEntry
    {
        public string Level { get; set; }

        public string Message { get; set; }

        public LogAttachment Attachment { get; set; }
    }
}
