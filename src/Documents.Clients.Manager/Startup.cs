namespace Documents.Clients.Manager
{
    using Documents.Clients.Manager.Common;
    using Documents.Clients.Manager.Modules;
    using Documents.Clients.Manager.Modules.AuditLog;
    using Documents.Clients.Manager.Services;
    using Documents.Common;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.HttpOverrides;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using NSwag.AspNetCore;
    using System.Reflection;

    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // see notes at: ConfigureJSONSerializerSettings()
            JsonConvert.DefaultSettings = () =>
            {
                var settings = new JsonSerializerSettings();
                ConfigureJSONSerializerSettings(settings);
                return settings;
            };

            // this may seem redundant, and it is, but MVC doesn't respect JsonConvert.DefaultSettings and we can't
            // replace the object in the options. :|
            services.AddMvc().
                AddJsonOptions(options =>
                {
                    ConfigureJSONSerializerSettings(options.SerializerSettings);
                });

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddTransient<FileService>();
            services.AddTransient<PathService>();
            services.AddTransient<FolderService>();
            services.AddTransient<SearchService>();
            services.AddTransient<MediaService>();
            services.AddTransient<AttributeService>();
            services.AddTransient<SchemaService>();
            services.AddTransient<DocumentSetService>();
            services.AddTransient<ImageService>();
            services.AddTransient<ViewSetService>();
            services.AddTransient<TranscriptService>();
            services.AddTransient<ClipService>();
            services.AddTransient<ModuleConfigurator>();

            services.AddTransient<EDiscovery>();
            services.AddTransient<EArraignment>();
            services.AddTransient<LEOUploadModule>();
            services.AddTransient<LogReaderModule>();
            services.AddTransient<LogReaderService>();
            services.AddTransient<MetadataAuditLogStore>();

            services.AddTransient<SSOJWTAuthentication>();

            services.AddTransient<APIConnection>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {

            /*app.Use(async (HttpContext context, Func<Task> next) =>
            {
                DateTime start = DateTime.Now;

                await next.Invoke(); //let the rest of the pipeline run

                Console.WriteLine(DateTime.Now.Subtract(start).TotalMilliseconds);
            });*/

            Logging.SetupLoggerFactory(loggerFactory);

            app.UseDefaultFiles();
            app.UseStaticFiles();

            // This sets up support for load-balancer SSL termination so that we can see the actual client's IP
            // and be aware of the protocol the client is using to communicate with the load balancer, even if 
            // we are only service HTTP between the LB and us
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseSwaggerUi(typeof(Startup).GetTypeInfo().Assembly, new SwaggerUiSettings 
            {
                IsAspNetCore = true,
                DefaultUrlTemplate = "api/{controller}/{action}/{id?}"
            });

            app.UseMvc(routes =>
            {
                routes.MapRoute("manager", "manager/{*id}",
                    defaults: new { controller = "Home", action = "Index" });

                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");

                routes.MapRoute(
                    name: "angular", 
                    template: "{*url}", 
                    defaults: new { controller = "Home", action = "Spa" });
            });
        }

        private void ConfigureJSONSerializerSettings(JsonSerializerSettings settings)
        {
            // These are the default settings for JSON serializer.  These settings can be overriden.
            // specifically these settings are used when serializing back and forth.  This correctly handles the "type" name, which should 
            // always be lowercase. 

            settings.Formatting = Formatting.Indented;
            /*settings.ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            };*/
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
        }

    }
}
