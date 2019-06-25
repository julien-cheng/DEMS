namespace Documents.Clients.Tools.Commands
{
    using Documents.Queues.Messages;
    using McMaster.Extensions.CommandLineUtils;
    using Newtonsoft.Json;
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    [Subcommand("status", typeof(Status))]
    [Subcommand("send", typeof(Send))]
    [Subcommand("pipe", typeof(Pipe))]
    class QueueCommand : CommandBase
    {
        class Status : CommandBase
        {
            protected async override Task ExecuteAsync()
            {
                var statuses = await API.Queue.GetStatus();

                Table("Queues", statuses.OrderBy(s => s.Name));
            }
        }

        class Send : CommandBase
        {
            [Required, Argument(0, Description = "Queue to send to")]
            public string Queue { get; }

            [Argument(1, Description = "Value to send")]
            public string Value { get; }

            [Option("--from-file")]
            public string FromFile { get; }

            public object GetObjectValue(string argumentValue)
            {
                object objValue;

                if (FromFile != null)
                {
                    using (var fs = new FileStream(FromFile, FileMode.Open))
                    using (var sr = new StreamReader(fs))
                        objValue = JsonConvert.DeserializeObject(sr.ReadToEnd());
                }
                else if (argumentValue != null)
                    objValue = JsonConvert.DeserializeObject(argumentValue);
                else
                {
                    objValue = JsonConvert.DeserializeObject(Console.In.ReadToEnd());
                }

                return objValue;
            }

            protected async override Task ExecuteAsync()
            {
                await API.Queue.EnqueueAsync(Queue, GetObjectValue(Value));
            }
        }

        class Pipe : CommandBase
        {

            [Required, Argument(0, Description = "Queue to send to")]
            public string Queue { get; }

            [Required, Argument(1, Description = "Executable to pipe the file into")]
            public string Executable { get; }

            [Option]
            public bool Stream { get; }

            [Option]
            public bool FileBased { get; }

            [Option]
            public string Extension { get; set; } = ".tmp";

            protected async override Task ExecuteAsync()
            {
                using (var subscription = await API.Queue.SubscribeAsync(Queue))
                {
                    Console.WriteLine("Connected to queue");
                    await subscription.ListenAsync(async (message) =>
                    {
                        string arguments = string.Empty;
                        string tempPath = null;

                        try
                        {
                            Console.WriteLine("new message: " + message.Message);

                            FileBasedMessage fileMessage = null;

                            if (FileBased)
                            {
                                fileMessage = JsonConvert.DeserializeObject<FileBasedMessage>(message.Message);
                                if (!Stream)
                                {
                                    if (!string.IsNullOrEmpty(Extension) && !Extension.StartsWith("."))
                                        Extension = "." + Extension;

                                    tempPath = Path.GetTempPath() + Guid.NewGuid().ToString() + Extension;
                                    arguments = tempPath;

                                    using (var tmpStream = System.IO.File.OpenWrite(tempPath))
                                        await API.File.DownloadAsync(fileMessage.Identifier, tmpStream);
                                }
                            }

                            var p = new Process()
                            {
                                StartInfo = new ProcessStartInfo
                                {
                                    Arguments = arguments,
                                    WorkingDirectory = Directory.GetCurrentDirectory(),
                                    FileName = Executable,
                                    UseShellExecute = false,
                                    RedirectStandardOutput = true,
                                    RedirectStandardInput = Stream,
                                    StandardOutputEncoding = Encoding.UTF8,
                                }
                            };

                            var stdout = new StringBuilder();

                            p.OutputDataReceived += (sender, args) =>
                                stdout.AppendLine(args.Data);


                            Console.WriteLine("Starting process");
                            p.Start();

                            if (Stream && fileMessage != null)
                            {
                                await API.File.DownloadAsync(fileMessage.Identifier, async (stream, cancel) =>
                                {
                                    await stream.CopyToAsync(p.StandardInput.BaseStream);
                                });
                            }

                            p.BeginOutputReadLine();
                            //await p.WaitForExitAsync();
                            p.WaitForExit();
                            Console.WriteLine($"Process exited {p.ExitCode}");

                            //var stdout = stdout.ToString();

                            await subscription.Ack(message, p.ExitCode == 0);

                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Exception: {e}");
                            await subscription.Ack(message, false);
                        }

                        if (tempPath != null)
                            try
                            {
                                System.IO.File.Delete(tempPath);
                            }
                            catch (Exception) { }
                    });

                    Console.WriteLine("Disconnected from queue");
                }
            }
        }
    }
}
