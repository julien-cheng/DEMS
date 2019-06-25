namespace Documents.API
{
    using Documents.API.Authentication;
    using Documents.API.Common;
    using Documents.API.Events;
    using Documents.API.Models.Binding;
    using Documents.API.Private;
    using Documents.API.Queue;
    using Documents.API.Services;
    using Documents.Common;
    using Documents.Search;
    using Documents.Search.ElasticSearch;
    using Documents.Store;
    using Documents.Store.SqlServer.Stores;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Diagnostics;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Newtonsoft.Json;
    using System;

    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container
        public void ConfigureServices(IServiceCollection services)
        {
            var configuration = services.BuildServiceProvider().GetService<DocumentsAPIConfiguration>();

            services.AddMvc().
                AddJsonOptions(options =>
                {
                    options.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.Indented;
                    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Serialize;
                    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                });

            services.AddMemoryCache();
            services.AddMvc();

            services.AddTransient<JWT, JWT>();

            /* Context */
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<ISecurityContext, TokenSecurityContext>();

            SetupStoreDriver(services, configuration);

            services.AddTransient<QueueProxy, QueueProxy>();
            services.AddTransient<QueueSender, QueueSender>();
            services.AddTransient<FileContentsService, FileContentsService>();

            /* Misecellaneous */
            services.AddTransient<IOrganizationPrivateMetadata, OrganizationPrivateMetadata>();
            services.AddTransient<IBackendClient, BackendClient>();
            services.AddTransient<IEventSender, EventSender>();
            services.AddTransient<IQueueSender, QueueSender>();

            if (configuration.RedisCacheEnabled)
                services.AddDistributedRedisCache(option =>
                {
                    option.Configuration = configuration.RedisConnection;
                    option.InstanceName = configuration.RedisInstanceName;

                });

            services.BuildServiceProvider().GetService<IQueueSender>().Initialize();


            SetupModelBindingDependencyInjection(services);

            // Setup authentication (see Documents.API.Authentication.JWTServicesExtensions)
            services.UseJWTAuthentication();
        }

        private void SetupStoreDriver(IServiceCollection services, DocumentsAPIConfiguration configuration)
        {
            /* Stores */
            services.AddTransient<IOrganizationStore, OrganizationStore>();
            services.AddTransient<IFolderStore, FolderStore>();
            services.AddTransient<IFileStore, FileStore>();
            services.AddTransient<IUserStore, UserStore>();
            services.AddTransient<IAuditLogEntryStore, AuditLogEntryStore>();
            services.AddTransient<IUploadStore, UploadStore>();
            services.AddTransient<IUploadChunkStore, UploadChunkStore>();
            services.AddTransient<IHealthStore, HealthStore>();

            services.AddTransient<ISearch>((sp) =>
                {
                    return new ElasticSearchDriver(
                        sp.GetService<ILogger<ElasticSearchDriver>>(),
                        configuration.ElasticSearchUri,
                        configuration.ElasticSearchIndex
                    );
                }
            );


            // todo: somehow needs to move into driver initialization
            /* EntityFramework */
            services.AddEntityFrameworkSqlServer()
                .AddDbContext<DocumentsContext>(options =>
                {
                    options.UseSqlServer(configuration.ConnectionString)
                        .ConfigureWarnings(warnings => warnings.Throw(CoreEventId.IncludeIgnoredWarning));
                });

            services.BuildServiceProvider().GetService<DocumentsContext>().Initialize();
        }

        private void SetupModelBindingDependencyInjection(IServiceCollection services)
        {
            // some super-trickery that enables DI-based json model binding
            // for the interfaces in our models.
            services.AddSingleton<IDIMeta>(s => { return new DIMetaDefault(services); });
            services.AddTransient<IConfigureOptions<MvcJsonOptions>, JsonOptionsSetup>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IApplicationLifetime appLifetime)
        {
            Logging.SetupLoggerFactory(loggerFactory);

            app.UseAuthentication();

            app.UseWebSockets(new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromMilliseconds(10000)
            });

            app.UseQueueHandler();



            // These are the default settings for JSON serializer.  These settings can be overriden.
            // specifically these settings are used when serializing back and forth.  This correctly handles the "type" name, which should 
            // always be lowercase. 

            JsonConvert.DefaultSettings = () =>
            {
                return new JsonSerializerSettings()
                {
                    Formatting = Formatting.Indented,
                    /*ContractResolver = new DefaultContractResolver
                    {
                        //NamingStrategy = new CamelCaseNamingStrategy()
                    },*/
                    NullValueHandling = NullValueHandling.Ignore,
                    ReferenceLoopHandling = ReferenceLoopHandling.Serialize
                };
            };

            app.UseMvc();
        }
    }
}

