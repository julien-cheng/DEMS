namespace Documents.Common
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Serilog;
    using System;

    public class Logging
    {
        private static ILoggerFactory LoggerFactory = new LoggerFactory();

        public static ILogger<TLogger> CreateLogger<TLogger>()
        {
            return LoggerFactory.CreateLogger<TLogger>();
        }

        public static Microsoft.Extensions.Logging.ILogger CreateLogger(Type t)
        {
            return LoggerFactory.CreateLogger(t);
        }

        internal static void Initialize(IConfigurationRoot configurationRoot)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .ReadFrom.Configuration(configurationRoot)                
                .CreateLogger();

            LoggerFactory.AddSerilog();
        }

        public static void SetupLoggerFactory(ILoggerFactory loggerFactory)
        {
            loggerFactory.AddSerilog();
        }

        public static void Flush()
        {
            Log.CloseAndFlush();
        }
    }
}
