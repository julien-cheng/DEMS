namespace Documents.Clients.Tools.Commands
{
    using Documents.Clients.Tools.Common;
    using McMaster.Extensions.CommandLineUtils;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    abstract class ModelMetadataCommandBase : CommandBase
    {
        public class MetadataRow
        {
            public string Source { get; set; }
            public string Target { get; set; }
            public string Key { get; set; }
            public string Value { get; set; }
        }

        public abstract class MetadataGetSetBase : CommandBase
        {
            [Argument(1)]
            public string Key { get; }

            protected IMetadataHandler Handler = null;
        }

        public abstract class MetadataGetBase : MetadataGetSetBase
        {
            protected async override Task ExecuteAsync()
            {
                var rows = new List<MetadataRow>();

                await Handler.PopulateRows(rows);

                if (Key == null)
                {
                    Table("Metadata", rows);
                }
                else
                {
                    var json = rows.LastOrDefault(r => r.Key == Key).Value;
                    if (json != null)
                    {
                        var obj = JsonConvert.DeserializeObject(json);
                        json = JsonConvert.SerializeObject(obj, Formatting.Indented);

                        Console.WriteLine(json);
                    }
                    else
                        Console.Write("null");
                }
            }
        }

        public abstract class MetadataSetBase : MetadataGetSetBase
        {
            [Option("--from-file")]
            public string FromFile { get; }

            [Option("--watch")]
            public bool Watch { get; }

            [Argument(2)]
            public string Value { get; }

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
                if (Watch && FromFile != null)
                {
                    var filename = Path.GetFullPath(FromFile);
                    Console.WriteLine($"filename: {filename}");
                    await Root.Connection.ConcurrencyRetryBlock(Handler.SetMetadata);
                    using (var watcher = new DirectoryMonitor(System.IO.Path.GetFileName(filename)))
                    {
                        watcher.Change += (string changedFilePath) =>
                        {
                            Task.Run(async () =>
                            {
                                Console.WriteLine("Updating metadata");
                                await Root.Connection.ConcurrencyRetryBlock(Handler.SetMetadata);
                            });
                        };
                        watcher.Start();

                        while (true)
                        {
                            Console.WriteLine("watching");
                            Console.ReadLine();
                        }
                    }
                }
                else
                    await Root.Connection.ConcurrencyRetryBlock(Handler.SetMetadata);
            }
        }


        public interface IMetadataHandler
        {
            Task PopulateRows(List<MetadataRow> rows);
            Task SetMetadata();
        }
    }
}
