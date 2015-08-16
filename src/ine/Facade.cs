using ine.Domain;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ine
{
    public class Facade
    {
        static Facade()
        {
            Log.Logger =
                new LoggerConfiguration()
                    .WriteTo.File(GetDataPath("logging.txt"))
                    .MinimumLevel.Verbose()
                    .CreateLogger();
        }

        private static string GetDataPath()
        {
            string directory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\adma\\ine";

            if (Directory.Exists(directory) == false)
            {
                Directory.CreateDirectory(directory);
            }

            return directory;
        }

        private static string GetDataPath(string filename)
        {
            return Path.Combine(GetDataPath(), filename);
        }

        private static string GetPhantomPath()
        {
            return Environment.CurrentDirectory + "\\phantomjs.exe";
        }

        private static string GetScriptPath(string hosting)
        {
            switch (hosting)
            {
                case "nitroflare.com":
                    return Environment.CurrentDirectory + "\\Scripts\\nitroflare.js";

                default:
                    throw new NotSupportedException();
            }
        }

        public Task<Link[]> ParseTextToLinks(string text)
        {
            return Task.Run(async () =>
            {
                var query =
                    from link in await this.ParseTextToLinks(text, true)
                    group link by link.Url into byUrl
                    select byUrl.First();

                return query.ToArray();
            });
        }

        private async Task<Link[]> ParseTextToLinks(string text, bool deep)
        {
            Uri uri;
            List<Link> links = new List<Link>();
            string pattern = @"(https?:\/\/)?([\da-z\.-]+)\.([a-z\.]{2,6})([\/\w \.-=]*)*\/?";

            if (String.IsNullOrWhiteSpace(text) == false)
            {
                foreach (Match match in Regex.Matches(text, pattern))
                {
                    if (Uri.TryCreate(match.Value, UriKind.Absolute, out uri) == true)
                    {
                        if (uri.Authority == "nitroflare.com" || uri.Authority == "www.nitroflare.com")
                        {
                            if (uri.Segments.Length >= 3)
                            {
                                if (uri.Segments[1].TrimEnd('/').ToLower() == "view")
                                {
                                    links.Add(new Link
                                    {
                                        Hosting = "nitroflare.com",
                                        Url = new Uri(match.Value.ToLower(), UriKind.Absolute)
                                    });
                                }
                                else if (uri.Segments[1].TrimEnd('/').ToLower() == "folder")
                                {
                                    using (WebClient client = new WebClient())
                                    {
                                        try
                                        {
                                            client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

                                            string post = String.Format("userId={0}&folder={1}&page=1&perPage=50", uri.Segments[2].Trim('/'), Uri.EscapeDataString(uri.Segments[3].Trim('/')));
                                            string data = await client.UploadStringTaskAsync("http://nitroflare.com/ajax/folder.php", post);
                                            string absolute = data.Replace(@"view\/", @"http://nitroflare.com/view/").Replace(@"\/", "/");
                                            Link[] found = await this.ParseTextToLinks(absolute, false);

                                            links.AddRange(found);
                                        }
                                        catch (WebException)
                                        {
                                            // ignore
                                        }
                                    }
                                }
                            }
                        }
                        else if (deep == true)
                        {
                            using (WebClient client = new WebClient())
                            {
                                try
                                {
                                    string data = await client.DownloadStringTaskAsync(uri);
                                    Link[] found = await this.ParseTextToLinks(data, false);

                                    links.AddRange(found);
                                }
                                catch (WebException)
                                {
                                    // ignore
                                }
                            }
                        }
                    }
                }
            }

            return links.ToArray();
        }

        public async Task Analyze(LinkTask task)
        {
            if (task.Links.Length > 0)
            {
                foreach (Link link in task.Links)
                {
                    task.OnStatus(link, "pending");
                }

                await Task.Run(() =>
                {
                    Parallel.ForEach(task.Links, new ParallelOptions { MaxDegreeOfParallelism = 2 }, link =>
                    {
                        task.OnStatus.Invoke(link, "checking");
                        task.OnLog.Invoke(link, new LogEntry { Level = "INFO", Message = String.Format("Analyzing '{0}'.", link.Url) });
                        task.OnLog.Invoke(link, new LogEntry { Level = "INFO", Message = "Starting PhantomJS" });

                        ProcessStartInfo info = new ProcessStartInfo
                        {
                            FileName = GetPhantomPath(),
                            Arguments = GetScriptPath(link.Hosting) + " query " + link.Url.ToString(),
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden,
                            WorkingDirectory = GetDataPath()
                        };

                        using (Process process = Process.Start(info))
                        {
                            Resource resource = new Resource
                            {
                                Url = link.Url,
                                Hosting = link.Hosting,
                                IsAvailable = true
                            };

                            while (process.StandardOutput.EndOfStream == false)
                            {
                                string line = process.StandardOutput.ReadLine();
                                string[] parts = line.Split(new[] { ':' }, 2);

                                switch (parts[0])
                                {
                                    case "file-name":
                                        resource.Name = parts[1].Trim();
                                        break;

                                    case "file-size":
                                        resource.Size = parts[1].Trim();
                                        break;

                                    case "file-status":
                                        resource.IsAvailable = false;
                                        break;

                                    case "fatal":
                                        task.OnLog.Invoke(link, new LogEntry { Level = "FATAL", Message = parts[1].Trim() });
                                        break;
                                }
                            }

                            if (resource.IsAvailable == false)
                            {
                                task.OnStatus(link, "unavailable");
                            }

                            if (resource.IsAvailable == true)
                            {
                                task.OnStatus(link, "available");
                            }

                            if (resource.Name != null && resource.Size != null)
                            {
                                task.OnCompleted(link, resource);
                            }

                            process.WaitForExit();
                        }
                    });
                });
            }
        }

        public void Download(ResourceTask task)
        {
            Task.Run(async () =>
            {
                try
                {
                    await this.AcquireSlot(task);

                    while (true)
                    {
                        PhantomResponse response = await CallPhantom(task);

                        if (response.Waiting != null)
                        {
                            await this.Wait(task, response.Waiting.Value + TimeSpan.FromMinutes(3));
                            continue;
                        }

                        if (response.DownloadUrl != null)
                        {
                            await this.DownloadFile(task, response.DownloadUrl);

                            task.OnCompleted.Invoke(true);
                            task.OnLog.Invoke(new LogEntry { Level = "INFO", Message = "Completed." });

                            break;
                        }

                        task.OnStatus(String.Empty);
                        task.OnCompleted.Invoke(false);
                        task.OnLog.Invoke(new LogEntry { Level = "WARN", Message = "Completed without downloading." });

                        break;
                    }
                }
                catch (TaskCanceledException)
                {
                    task.OnStatus("timeout");
                    task.OnCompleted.Invoke(false);
                    task.OnLog.Invoke(new LogEntry { Level = "WARN", Message = "Solving captcha timed out." });
                }
                catch (OperationCanceledException)
                {
                    task.OnStatus("cancelled");
                    task.OnCompleted.Invoke(false);
                    task.OnLog.Invoke(new LogEntry { Level = "WARN", Message = "Downloading was cancelled." });
                }
                catch (Exception ex)
                {
                    task.OnStatus("failed");
                    task.OnCompleted.Invoke(false);
                    task.OnLog.Invoke(new LogEntry { Level = "ERROR", Message = "Downloading failed. " + ex.Message });
                }
                finally
                {
                    this.ReleaseSlot(task);
                }
            });
        }

        private class PhantomResponse
        {
            public List<string> Lines;
            public TimeSpan? Waiting;
            public string DownloadUrl;
        }

        private async Task AcquireSlot(ResourceTask task)
        {
            bool succeeded = task.Scheduler.Schedule(task.Hosting);
            TimeSpan period = TimeSpan.FromSeconds(5);

            task.OnStatus.Invoke("pending");
            task.OnLog(new LogEntry { Level = "INFO", Message = String.Format("Scheduling '{0}'.", task.Url) });

            while (succeeded == false)
            {
                await Task.Delay(period, task.Cancellation);
                succeeded = task.Scheduler.Schedule(task.Hosting);
            }

            task.OnLog(new LogEntry { Level = "INFO", Message = String.Format("Url '{0}' acquired download slot.", task.Url) });
        }

        private void ReleaseSlot(ResourceTask task)
        {
            task.Scheduler.Release(task.Hosting);
        }

        private async Task<PhantomResponse> CallPhantom(ResourceTask task)
        {
            Regex regex = new Regex(@"to wait (?<minutes>[0-9]+) minutes");
            PhantomResponse response = new PhantomResponse
            {
                Lines = new List<string>()
            };

            task.OnStatus.Invoke("starting");
            task.OnLog.Invoke(new LogEntry { Level = "INFO", Message = "Starting PhantomJS." });

            ProcessStartInfo info = new ProcessStartInfo
            {
                FileName = GetPhantomPath(),
                Arguments = GetScriptPath(task.Hosting) + " download " + task.Url.ToString(),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = GetDataPath()
            };

            string solution = null;
            Process process = Process.Start(info);

            try
            {
                task.OnStatus("working");

                while (process.StandardOutput.EndOfStream == false)
                {
                    task.Cancellation.ThrowIfCancellationRequested();

                    string line = process.StandardOutput.ReadLine();
                    string[] parts = line.Split(new[] { ':' }, 2);

                    response.Lines.Add(line);
                    switch (parts[0])
                    {
                        case "captcha-url":
                            solution = parts[1].Trim();
                            break;

                        case "download-url":
                            response.DownloadUrl = parts[1].Trim();
                            break;

                        case "message":
                            {
                                Match match = regex.Match(line);

                                if (match.Success == true)
                                {
                                    if (match.Groups["minutes"].Success == true)
                                    {
                                        response.Waiting = TimeSpan.FromMinutes(Int32.Parse(match.Groups["minutes"].Value));
                                    }
                                }
                            }

                            break;

                        case "debug":
                        case "request":
                            task.OnLog.Invoke(new LogEntry { Level = "DEBUG", Message = line });
                            break;

                        case "fatal":
                            task.OnLog.Invoke(new LogEntry { Level = "FATAL", Message = parts[1].Trim() });
                            break;
                    }

                    if (String.IsNullOrWhiteSpace(solution) == false)
                    {
                        task.Cancellation.ThrowIfCancellationRequested();
                        task.OnLog.Invoke(new LogEntry { Level = "INFO", Message = "Handling captcha." });

                        using (WebClient client = new WebClient())
                        {
                            task.OnStatus("decaptching");

                            TimeSpan timeout = TimeSpan.FromMinutes(3);
                            CancellationTokenSource source = CancellationTokenSource.CreateLinkedTokenSource(new CancellationTokenSource(timeout).Token, task.Cancellation);
                            Captcha captcha = new Captcha
                            {
                                Type = "image",
                                Data = client.DownloadData(solution),
                                Cancellation = source.Token
                            };

                            captcha.Reload = async () =>
                            {
                                process.StandardInput.WriteLine("::reload::");
                                task.OnLog.Invoke(new LogEntry { Level = "INFO", Message = "Reloading captcha." });

                                do
                                {
                                    line = await process.StandardOutput.ReadLineAsync();
                                }
                                while (line.StartsWith("captcha-url: ") == false);

                                source = CancellationTokenSource.CreateLinkedTokenSource(new CancellationTokenSource(timeout).Token, task.Cancellation);
                                captcha.Cancellation = source.Token;
                                captcha.Data = await client.DownloadDataTaskAsync(line.Substring("captcha-url: ".Length));
                            };

                            captcha.ToAudio = async () =>
                            {
                                process.StandardInput.WriteLine("::audio::");
                                task.OnLog.Invoke(new LogEntry { Level = "INFO", Message = "Switching to audio." });

                                do
                                {
                                    line = await process.StandardOutput.ReadLineAsync();
                                }
                                while (line.StartsWith("captcha-url: ") == false);

                                source = CancellationTokenSource.CreateLinkedTokenSource(new CancellationTokenSource(timeout).Token, task.Cancellation);
                                captcha.Cancellation = source.Token;
                                captcha.Data = await client.DownloadDataTaskAsync(line.Substring("captcha-url: ".Length));
                                captcha.Type = "audio";
                            };

                            captcha.ToImage = async () =>
                            {
                                process.StandardInput.WriteLine("::image::");
                                task.OnLog.Invoke(new LogEntry { Level = "INFO", Message = "Switching to image." });

                                do
                                {
                                    line = await process.StandardOutput.ReadLineAsync();
                                }
                                while (line.StartsWith("captcha-url: ") == false);

                                source = CancellationTokenSource.CreateLinkedTokenSource(new CancellationTokenSource(timeout).Token, task.Cancellation);
                                captcha.Cancellation = source.Token;
                                captcha.Data = await client.DownloadDataTaskAsync(line.Substring("captcha-url: ".Length));
                                captcha.Type = "image";
                            };

                            solution = await task.OnCaptcha.Invoke(captcha);
                            task.OnStatus("working");
                        }

                        task.Cancellation.ThrowIfCancellationRequested();
                        task.OnLog.Invoke(new LogEntry { Level = "INFO", Message = "Sending captcha." });

                        process.StandardInput.WriteLine(solution);
                        solution = null;
                    }
                }

                process.WaitForExit();
                return response;
            }
            finally
            {
                if (process.HasExited == false)
                {
                    process.Kill();
                }

                process.Dispose();
            }
        }

        private async Task Wait(ResourceTask task, TimeSpan waiting)
        {
            TimeSpan counter = waiting;

            if (counter.TotalMinutes > 20)
            {
                counter = TimeSpan.FromMinutes(20);
            }

            task.OnLog.Invoke(new LogEntry { Level = "INFO", Message = "Waiting." });

            while (counter.TotalMinutes > 0)
            {
                task.OnStatus(String.Format("{0} / {1}", Math.Round(waiting.TotalMinutes), Math.Round(counter.TotalMinutes)));

                await Task.Delay(TimeSpan.FromMinutes(1), task.Cancellation);
                waiting = waiting - TimeSpan.FromMinutes(1);
                counter = counter - TimeSpan.FromMinutes(1);
            }
        }

        private async Task DownloadFile(ResourceTask task, string url)
        {
            DateTime started = DateTime.Now;
            List<Tuple<DateTime, long>> points = new List<Tuple<DateTime, long>>();

            using (WebClient client = new WebClient())
            {
                client.DownloadProgressChanged += (sender, args) =>
                {
                    task.OnProgress(args.BytesReceived, args.TotalBytesToReceive);

                    double elapsed = (DateTime.Now - started).TotalSeconds;
                    long downloaded = args.BytesReceived;
                    long left = (args.TotalBytesToReceive - args.BytesReceived);

                    task.OnEstimation(TimeSpan.FromSeconds(elapsed * left / downloaded));

                    lock (points)
                    {
                        points.Add(Tuple.Create(DateTime.Now, args.BytesReceived));

                        if (points.Count == 256)
                        {
                            points.RemoveAt(0);
                        }

                        if (points.Count >= 2)
                        {
                            var lowest = points[0];
                            var highest = points[points.Count - 1];

                            if ((highest.Item1 - lowest.Item1).TotalSeconds > 0)
                            {
                                task.OnSpeed(Convert.ToInt64(Math.Round((highest.Item2 - lowest.Item2) / (highest.Item1 - lowest.Item1).TotalSeconds)));
                            }
                        }
                    }
                };

                task.OnStatus.Invoke("downloading");
                task.OnLog.Invoke(new LogEntry { Level = "INFO", Message = "Downloading file." });

                await client.DownloadFileTaskAsync(url, task.Destination);
                task.Cancellation.Register(client.CancelAsync);
                task.OnStatus("completed");
            }
        }

        public Task<Resource[]> GetAllResources()
        {
            return Task.Run(() =>
            {
                List<Resource> resources = new List<Resource>();

                using (FileStream stream = new FileStream(GetDataPath("transfers.txt"), FileMode.OpenOrCreate, FileAccess.Read, FileShare.None, 16 * 1024, FileOptions.None))
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    while (reader.EndOfStream == false)
                    {
                        string[] parts = reader.ReadLine().Split(new[] { '|' }, 4);
                        if (parts.Length == 4)
                        {
                            resources.Add(new Resource
                            {
                                Hosting = parts[0],
                                Name = parts[1],
                                Size = parts[2],
                                Url = new Uri(parts[3], UriKind.Absolute)
                            });
                        }
                    }
                }

                return resources.ToArray();
            });
        }

        public Task PersisteResources(Resource[] resources)
        {
            return Task.Run(() =>
            {
                using (FileStream stream = new FileStream(GetDataPath("transfers.txt"), FileMode.Create, FileAccess.Write, FileShare.None, 16 * 1024, FileOptions.WriteThrough))
                using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    foreach (Resource resource in resources)
                    {
                        writer.Write(resource.Hosting);
                        writer.Write('|');
                        writer.Write(resource.Name);
                        writer.Write('|');
                        writer.Write(resource.Size);
                        writer.Write('|');
                        writer.Write(resource.Url);
                        writer.WriteLine();
                    }

                    writer.Flush();
                    stream.Flush();
                }
            });
        }

        public void PersistLogs(params LogEntry[] entries)
        {
            foreach (LogEntry entry in entries)
            {
                switch (entry.Level)
                {
                    case "DEBUG":
                        Log.Debug(entry.Message);
                        break;

                    case "INFO":
                        Log.Information(entry.Message);
                        break;

                    case "WARN":
                        Log.Warning(entry.Message);
                        break;

                    case "ERROR":
                        Log.Error(entry.Message);
                        break;

                    case "FATAL":
                        Log.Fatal(entry.Message);
                        break;
                }
            }
        }

        public void Handle(Exception ex, string reason)
        {
            Log.Fatal(ex, reason);
        }
    }
}
