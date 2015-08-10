using ine.Domain;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ine
{
    public class Facade
    {
        public async Task<Link[]> ParseTextToLinks(string text)
        {
            Uri uri;
            List<Link> links = new List<Link>();
            string pattern = @"(https?:\/\/)?([\da-z\.-]+)\.([a-z\.]{2,6})([\/\w \.-]*)*\/?";

            if (String.IsNullOrWhiteSpace(text) == false)
            {
                foreach (Match match in Regex.Matches(text, pattern))
                {
                    if (Uri.TryCreate(match.Value.ToLower(), UriKind.Absolute, out uri) == true)
                    {
                        if (uri.Authority == "nitroflare.com" || uri.Authority == "www.nitroflare.com")
                        {
                            links.Add(new Link
                            {
                                Hosting = "nitroflare.com",
                                Url = uri
                            });
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
                        task.OnStatus(link, "checking");

                        ProcessStartInfo info = new ProcessStartInfo
                        {
                            FileName = @"D:\Drive\Projects\phantomjs.exe",
                            Arguments = @"D:\Drive\Projects\nitroflare.js query " + link.Url.ToString(),
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden
                        };

                        Process process = Process.Start(info);
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
                    });
                });
            }
        }

        public Task Download(ResourceTask task)
        {
            return Task.Run(async () =>
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
                            break;
                        }

                        task.OnStatus(String.Empty);
                        break;
                    }
                }
                catch (OperationCanceledException)
                {
                    task.OnStatus("cancelled");
                }
                catch
                {
                    task.OnStatus("failed");
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

            task.OnStatus("pending");

            while (succeeded == false)
            {
                await Task.Delay(period);
                succeeded = task.Scheduler.Schedule(task.Hosting);
            }
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

            task.OnStatus("starting");

            ProcessStartInfo info = new ProcessStartInfo
            {
                FileName = @"D:\Drive\Projects\phantomjs.exe",
                Arguments = @"D:\Drive\Projects\nitroflare.js download " + task.Url.ToString(),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            string captcha = null;
            Process process = Process.Start(info);

            task.OnStatus("working");

            while (process.StandardOutput.EndOfStream == false)
            {
                string line = process.StandardOutput.ReadLine();
                string[] parts = line.Split(new[] { ':' }, 2);

                response.Lines.Add(line);
                switch (parts[0])
                {
                    case "captcha-url":
                        captcha = parts[1];
                        break;

                    case "download-url":
                        response.DownloadUrl = parts[1];
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

                    case "fatal":
                        break;
                }

                if (String.IsNullOrWhiteSpace(captcha) == false)
                {
                    using (WebClient client = new WebClient())
                    {
                        task.OnStatus("decaptching");

                        try
                        {
                            TimeSpan timeout = TimeSpan.FromMinutes(3);
                            CancellationTokenSource source = new CancellationTokenSource(timeout);

                            captcha = await task.OnCaptcha.Invoke(client.DownloadData(captcha), source.Token);
                            task.OnStatus("working");
                        }
                        catch (TaskCanceledException)
                        {
                            task.OnStatus("timeout");
                            process.Kill();
                            break;
                        }
                    }

                    process.StandardInput.WriteLine(captcha);
                    captcha = null;
                }
            }

            process.WaitForExit();
            return response;
        }

        private async Task Wait(ResourceTask task, TimeSpan waiting)
        {
            TimeSpan counter = waiting;

            if (counter.TotalMinutes > 20)
            {
                counter = TimeSpan.FromMinutes(20);
            }

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
            List<Tuple<DateTime, long>> points = new List<Tuple<DateTime, long>>();

            using (WebClient client = new WebClient())
            {
                client.DownloadProgressChanged += (sender, args) =>
                {
                    task.OnProgress(args.BytesReceived, args.TotalBytesToReceive);

                    lock (points)
                    {
                        points.Add(Tuple.Create(DateTime.Now, args.BytesReceived));

                        if (points.Count == 50)
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

                task.OnStatus("downloading");
                await client.DownloadFileTaskAsync(url, task.Destination);
                task.OnStatus("completed");
            }
        }
    }
}
