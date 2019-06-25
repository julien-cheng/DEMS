
namespace Documents.Common.WebHost
{
    using Documents.Common;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using System.IO;
    using System.Threading;

    public class WebProcessEntry
    {
        public static void Entry<TStartup, TConfig>()
            where TStartup: class
            where TConfig: class, IDocumentsWebConfiguration, new()
        {
            ProcessEntry.Entry();

            var config = new TConfig();
            Configuration<TConfig>.Load(config.SectionName, instance: config);


            ThreadPool.SetMinThreads(50, 50);
            var host = new WebHostBuilder()
                .UseKestrel(options => {
                    options.Limits.MaxRequestBodySize = 250 * 1024 * 1024;
                    options.Limits.MinResponseDataRate = null;
                    options.Limits.MaxConcurrentConnections = null;
                    options.Limits.MaxConcurrentUpgradedConnections = null;
                })
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureServices((services) =>
                {
                    services.AddSingleton(config);
                })
                .UseStartup<TStartup>()
                .UseUrls(config.HostingURL)
                .Build();

            host.Run();
        }
    }
}
