using ine.Domain;
using System.Diagnostics;
using System.IO;

namespace ine.Extensions
{
    public static class ShellExtensions
    {
        public static void Show(this LogEntry entry)
        {
            string extension;

            switch (entry.Attachment.Type)
            {
                case "text":
                    extension = ".txt";
                    break;

                case "image":
                    extension = ".png";
                    break;

                case "audio":
                    extension = ".mp3";
                    break;

                default:
                    return;
            }

            string path = Path.GetTempFileName();
            string renamed = Path.ChangeExtension(path, extension);

            File.Move(path, renamed);
            File.WriteAllBytes(renamed, entry.Attachment.Data);
            Process.Start(renamed);
        }
    }
}
