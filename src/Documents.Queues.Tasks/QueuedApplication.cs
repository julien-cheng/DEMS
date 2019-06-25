namespace Documents.Queues.Tasks
{
    using Documents.API.Client;
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;
    using Documents.Common;
    using Documents.Queues.Tasks.Configuration;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Serilog.Context;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using PeterKottas.DotNetCore.WindowsService.Interfaces;
    using PeterKottas.DotNetCore.WindowsService;
    using System.Reflection;

    public abstract class QueuedApplication : IDisposable
    {
        protected ILogger Logger { get; set; }
        protected Connection API { get; set; }
        protected QueueSubscription Subscription { get; set; }
        protected List<string> LocalFilesToDelete = new List<string>();
        protected string LocalFilePath;

        protected QueueMessage CurrentMessageRaw = null;

        protected virtual string ConfigurationSectionName { get; }
        protected virtual string QueueName { get; }

        protected TaskConfiguration BaseConfiguration { get; set; }

        public static Task<int> StartAsync(Func<IEnumerable<LaunchProfile>> getLaunches)
        {
            ProcessEntry.Entry();
            ServiceRunner<MicroService>.Run(config =>
            {
                var name = Assembly.GetEntryAssembly().GetName().Name;
                config.SetName(name);
                config.SetDisplayName(name);
                config.SetDescription(name);
                var assemblyPath = Path.GetDirectoryName(
                    Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path));

                Configuration<TaskConfiguration>.Load("Documents", basePath: assemblyPath);
                var logger = Logging.CreateLogger(typeof(MicroService));
                config.Service(serviceConfig =>
                {
                    serviceConfig.ServiceFactory((extraArguments, controller) =>
                    {
                        return new MicroService(
                            logger: logger,
                            entry: async cancel =>
                            {
                                logger.LogInformation("Starting QueuedApplication StartAsync:Entry");
                                var tasks = new List<Task>();
                                var applications = new List<QueuedApplication>();

                                var launches = getLaunches();

                                while (!cancel.IsCancellationRequested)
                                {
                                    if (applications.Any())
                                    {
                                        foreach (var application in applications)
                                        {
                                            try
                                            {
                                                ((IDisposable)application).Dispose();
                                            }
                                            catch (Exception) { }
                                        }
                                        applications.Clear();
                                    }

                                    foreach (var launch in launches)
                                    {
                                        if (launch.FixedInstanceCount == 0)
                                            continue;

                                        // here's the first one
                                        var app = Activator.CreateInstance(launch.ApplicationType) as QueuedApplication;
                                        applications.Add(app);
                                        var configuration = Configuration<TaskConfiguration>.Load(app.ConfigurationSectionName).Object;

                                        // loop starts at second instance
                                        for (int i = 1; i < (launch.FixedInstanceCount ?? configuration.ConcurrentInstances); i++)
                                            applications.Add(Activator.CreateInstance(launch.ApplicationType) as QueuedApplication);

                                    }

                                    logger.LogInformation("launches.count:" + launches.Count());
                                    logger.LogInformation("applications.count:" + applications.Count());

                                    await Task.WhenAny(
                                        applications.Select(a =>
                                        {
                                            //Console.WriteLine($"Starting QueuedApplication {a.QueueName}");
                                            try
                                            {
                                                return a.RunAsync(cancel);
                                            }
                                            catch (Exception e)
                                            {
                                                logger.LogError(e, "Exception running application: " + e);
                                                throw;
                                            }
                                        }).ToArray()
                                    );
                                }
                            });
                    });

                    serviceConfig.OnStart((service, extraParams) =>
                    {
                        logger.LogInformation("Service {0} started", name);
                        ((IMicroService)service).Start();
                    });

                    serviceConfig.OnStop(service =>
                    {
                        logger.LogInformation("Service {0} stopped", name);
                        ((IMicroService)service).Stop();
                    });

                    serviceConfig.OnError(e =>
                    {
                        logger.LogError("Service {0} errored with exception : {1}\n{2}", name, e.Message, e.ToString());
                    });

                });
            });

            return Task.FromResult(0);
        }

        public async static Task<int> StartAsync(params Task[] tasks)
        {
            ProcessEntry.Entry();
            await Task.WhenAny(tasks);

            return 0;
        }

        public async static Task RunAsync<TApplication>(CancellationToken cancellationToken = default(CancellationToken), int? instances = null)
            where TApplication : QueuedApplication, new()
        {
            if (instances == 0)
                return;

            var apps = new List<QueuedApplication>();

            // here's the first one
            var app = new TApplication();
            apps.Add(app);
            var configuration = Configuration<TaskConfiguration>.Load(app.ConfigurationSectionName).Object;

            // loop starts at second instance
            for (int i = 1; i < (instances ?? configuration.ConcurrentInstances); i++)
                apps.Add(new TApplication());

            await Task.WhenAny(
                apps.Select(a =>
                {
                    //Console.WriteLine($"Starting QueuedApplication {a.QueueName}");
                    return a.RunAsync(cancellationToken);
                }).ToArray()
            );

            Logging.Flush();
        }

        protected virtual void Configure()
        {
            BaseConfiguration = Configuration<TaskConfiguration>.Load(ConfigurationSectionName).Object;

            Logger = Logging.CreateLogger(this.GetType());
        }

        public async Task<string> DownloadAsync(
            FileIdentifier fileIdentifier,
            string fileExtensionWithDot = ".tmp",
            CancellationToken cancellation = default(CancellationToken))
        {
            if (!fileIdentifier.IsValid)
                throw new ArgumentException($"{nameof(fileIdentifier)} is not valid");

            LocalFilePath = this.CreateTemporaryFile(fileExtensionWithDot);
            LocalFilesToDelete.Add(LocalFilePath);

            using (var fileStream = new FileStream(
                    LocalFilePath,
                    FileMode.OpenOrCreate,
                    FileAccess.Write
                ))
                await API.File.DownloadAsync(fileIdentifier, fileStream);

            return LocalFilePath;
        }

        public string CreateTemporaryFile(string extensionWithDot = ".tmp")
        {
            var tempPath = Path.GetTempPath() + Guid.NewGuid().ToString() + extensionWithDot;
            LocalFilesToDelete.Add(tempPath);
            return tempPath;
        }

        protected abstract Task Process();

        protected virtual void CleanupTempFiles()
        {
            foreach (var path in LocalFilesToDelete)
                try
                {
                    if (File.Exists(path) && (File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory)
                        Directory.Delete(path);
                    else
                        File.Delete(path);
                }
                catch (Exception) { }

            LocalFilesToDelete.Clear();
        }

        protected Task SetComplete()
            => Subscription.Ack(
                message: CurrentMessageRaw,
                success: true
            );

        protected async Task SetFailed(string reason) // todo: dropping "reason"
        {
            Logger.LogError("TaskFailed {@reason}", reason);

            await Subscription.Ack(
                message: CurrentMessageRaw,
                success: false
            );
        }

        protected async Task TagAlternativeView(FileIdentifier originalFileIdentifier, FileIdentifier newFileIdentifier, AlternativeView alternativeView)
        {
            await this.API.ConcurrencyRetryBlock(async () =>
            {
                var originalFile = await API.File.GetAsync(originalFileIdentifier);

                var views = originalFile.Read<List<AlternativeView>>(MetadataKeyConstants.ALTERNATIVE_VIEWS) ?? new List<AlternativeView>();
                views.Add(alternativeView);
                originalFile.Write(MetadataKeyConstants.ALTERNATIVE_VIEWS, views);

                await API.File.PutAsync(originalFile);
            });
        }

        protected async Task<TokenResponseModel> Connect(CancellationToken cancellationToken)
        {
            try
            {
                API = new Connection(new Uri(BaseConfiguration.API.Uri))
                {
                    Logger = Logging.CreateLogger<Connection>()
                };

                var tokenResponse = await API.User.AuthenticateAsync(new TokenRequestModel
                {
                    Identifier = new UserIdentifier(
                        BaseConfiguration.API.OrganizationKey,
                        BaseConfiguration.API.UserKey
                    ),
                    Password = BaseConfiguration.API.Password
                }, cancellationToken: cancellationToken);

                return tokenResponse;
            }
            catch (Exception e)
            {
                Logger.LogError(0, e, "Failed to connect to API");
                throw new Exception("Failed to connect to API", e);
            }
        }

        public async Task RunAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            Configure();

            if (this.BaseConfiguration.StartupDelay > 0)
            {
                Logger.LogInformation($"Delaying startup by {this.BaseConfiguration.StartupDelay}ms");
                await Task.Delay(this.BaseConfiguration.StartupDelay);
                Logger.LogInformation($"Delaying complete");
            }
            using (LogContext.PushProperty("pid", System.Diagnostics.Process.GetCurrentProcess().Id))
            using (LogContext.PushProperty("queue", QueueName))
            {
                Logger.LogInformation(
                    $"ProcessID: {System.Diagnostics.Process.GetCurrentProcess().Id}\n" +
                    $"QueuedApplication Starting: {this.QueueName}\n" +
                    $"API: {this.BaseConfiguration?.API?.Uri} User: {this.BaseConfiguration?.API?.UserKey}"
                );

                while (!cancellationToken.IsCancellationRequested)
                {
                    bool failed = false;
                    bool reentered = false;
                    bool fatal = false;

                    try
                    {
                        await Connect(cancellationToken);

                        using (Subscription = await API.Queue.SubscribeAsync(QueueName))
                        {
                            Logger.LogInformation($"Connected to Queue {QueueName}");

                            await Subscription.ListenAsync(async (message) =>
                            {

                                failed = false;
                                reentered = false;
                                fatal = false;

                                using (LogContext.PushProperty("messageID", message.ID))
                                {
                                    var started = DateTime.UtcNow;

                                    Logger.LogDebug($"Message: ID:{message.ID}");

                                    CurrentMessageRaw = message;

                                    if (CurrentMessageRaw != null)
                                    {

                                        string failure = null;
                                        try
                                        {
                                            await OnMessage(message);
                                        }
                                        catch (TaskReentranceException)
                                        {
                                            failed = false;
                                            reentered = true;
                                        }
                                        catch (TaskValidationException e)
                                        {
                                            failed = true;
                                            failure = e.Message;
                                        }
                                        catch (Exception e)
                                        {
                                            failed = true;
                                            failure = e.Message;
                                            Logger.LogError(e, $"Exception: {e}");

                                            if (BaseConfiguration.UnhandledExceptionsFatal)
                                            {
                                                fatal = BaseConfiguration.UnhandledExceptionsFatal;
                                                throw;
                                            }
                                        }
                                        finally
                                        {
                                            if (failed)
                                            {
                                                await SetFailed(
                                                    $"FAILED: {failure}"
                                                );

                                                if (fatal)
                                                    await Subscription.Close();
                                            }
                                            else
                                            {
                                                if (BaseConfiguration.LogCompletion)
                                                {
                                                    var elapsed = (int)DateTime.UtcNow.Subtract(started).TotalMilliseconds;

                                                    this.LogCompletion(elapsed, reentered);
                                                }

                                                Logger.LogDebug($"Complete.");

                                                await SetComplete();
                                            }

                                            CleanupTempFiles();
                                        }
                                    }
                                }
                            },
                            cancellationToken: cancellationToken);
                        }
                    }
                    catch (TaskCanceledException) { }
                    catch (Exception)
                    {
                        if (BaseConfiguration.UnhandledExceptionsFatal)
                            throw;
                    }

                    Logger.LogInformation($"Disconnected from Queue {QueueName}");

                    if (!cancellationToken.IsCancellationRequested
                        && failed
                        && BaseConfiguration.RestartDelay > 0)
                    {
                        Logger.LogInformation($"Delaying {BaseConfiguration.RestartDelay} before reconnecting");
                        await Task.Delay(BaseConfiguration.RestartDelay);
                    }
                }
            }
        }

        protected virtual void LogCompletion(int elapsed, bool reentered)
        {
            if (reentered)
                Logger.LogInformation("TaskReentered {@details} {@taskElapsedMS}", this.TaskCompletionDetails(), elapsed);
            else
                Logger.LogInformation("TaskComplete {@details} {@taskElapsedMS}", this.TaskCompletionDetails(), elapsed);
        }

        protected virtual object TaskCompletionDetails()
        {
            return new
            {
                this.QueueName
            };
        }

        protected virtual async Task OnMessage(QueueMessage message)
        {
            await Process();
        }

        protected bool HasAlternativeView(FileModel fileModel, Func<AlternativeView, bool> where)
        {
            var views = fileModel.Read<List<AlternativeView>>(MetadataKeyConstants.ALTERNATIVE_VIEWS);
            if (views != null)
                return views.Any(v => where(v));
            else
                return false;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    ((IDisposable)Subscription).Dispose();
                    CleanupTempFiles();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~QueuedApplication() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        void IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

    }

    public abstract class QueuedApplication<TConfig, TMessage>
        : QueuedApplication
        where TConfig : TaskConfiguration, IDocumentsConfiguration, new()
        where TMessage : class
    {
        protected TMessage CurrentMessage = null;
        protected TConfig Configuration { get; set; }

        // for logging purposes only
        protected FileModel LastFileModelLogging = null;

        protected override void Configure()
        {
            var configuration = Configuration<TConfig>.Load(ConfigurationSectionName);
            BaseConfiguration = Configuration = configuration.Object;

            Logger = Logging.CreateLogger(this.GetType());
        }

        protected override async Task OnMessage(QueueMessage message)
        {
            CurrentMessage = JsonConvert.DeserializeObject<TMessage>(message.Message);
            await Process();
            CurrentMessageRaw.Message = JsonConvert.SerializeObject(CurrentMessage);
        }

        private TIdentifier IdentifierFromMessage<TIdentifier>()
            where TIdentifier : class, IIdentifier
        {
            if (CurrentMessage is TIdentifier)
                return CurrentMessage as TIdentifier;
            else
            {
                var obj = CurrentMessage as IHasIdentifier<TIdentifier>;

                return obj?.Identifier;
            }
        }

        public async Task<FileModel> GetFileAsync(
                FileIdentifier fileIdentifier,
                CancellationToken cancellationToken = default(CancellationToken)
            )
        {
            LastFileModelLogging = await API.File.GetAsync(fileIdentifier, cancellationToken: cancellationToken);
            ValidateFile(LastFileModelLogging);
            return LastFileModelLogging;
        }

        protected virtual void ValidateFile(FileModel fileModel)
        {
            if (fileModel == null)
                throw new TaskValidationException("File does not exist");

            if (Configuration.MaximumInputFileSize > 0)
                if (fileModel.Length > Configuration.MaximumInputFileSize)
                    throw new TaskValidationException("File is over Maximum File Size");

            if (Configuration.OutputViewName != null)
                if (HasAlternativeView(fileModel, v => v.Name == Configuration.OutputViewName))
                    throw new TaskReentranceException();
        }

        public Task<FileModel> GetFileAsync()
        {
            return GetFileAsync(
                IdentifierFromMessage<FileIdentifier>()
                    ?? throw new Exception("Cannot GetFile from CurrentMessage type because it is not a FileIdentifier")
            );
        }

        public Task<string> DownloadAsync(string fileExtensionWithDot = ".tmp")
        {
            return DownloadAsync(
                IdentifierFromMessage<FileIdentifier>()
                    ?? throw new Exception("Cannot Download from CurrentMessage type because it is not a FileIdentifier"),
                fileExtensionWithDot
            );
        }

        protected override object TaskCompletionDetails()
        {
            return new
            {
                QueueName,
                CurrentMessage,
                LastFileModelLogging?.Identifier.OrganizationKey,
                LastFileModelLogging?.Identifier.FolderKey,
                LastFileModelLogging?.Identifier.FileKey,
                LastFileModelLogging?.Length,
                LastFileModelLogging?.Name,
                LastFileModelLogging?.MimeType
            };
        }
    }
}
