namespace Documents.Clients.Tools
{
    using Documents.Clients.Tools.Commands;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    static class Configuration
    {
        private const string PROFILE_SUBFOLDER = ".dms";
        private const string CONFIG_FILE = "config.json";

        public static string ConfigurationFilePath()
        {
            var path = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.UserProfile
                ),
                PROFILE_SUBFOLDER);

            return Path.Combine(path, CONFIG_FILE);
        }

        public static void Save(ConfigurationRoot.ToolsConfiguration configuration)
        {
            var fileName = ConfigurationFilePath();
            var path = Path.GetDirectoryName(fileName);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            File.WriteAllText(fileName, JsonConvert.SerializeObject(configuration, Formatting.Indented));
        }

        public static (ConfigurationRoot.ToolsConfiguration, ConfigurationRoot.ToolsConfiguration.DocumentsAPIConfiguration) Load(RootCommand root)
        {

            var config = new ConfigurationRoot
            {
                DocumentsClientsTools = new ConfigurationRoot.ToolsConfiguration
                {
                    Contexts = new Dictionary<string, ConfigurationRoot.ToolsConfiguration.DocumentsAPIConfiguration>()
                }
            };

            var globalConfigPath = System.Environment.GetEnvironmentVariable("DOCUMENTS_CONFIG_PATH");
            if (globalConfigPath != null)
                if (File.Exists(globalConfigPath))
                    JsonConvert.PopulateObject(File.ReadAllText(globalConfigPath), config);

            var userConfigPath = ConfigurationFilePath();
            if (File.Exists(userConfigPath))
                JsonConvert.PopulateObject(File.ReadAllText(userConfigPath), config.DocumentsClientsTools);

            ConfigurationRoot.ToolsConfiguration.DocumentsAPIConfiguration context = null;
            var activeContextName = config.DocumentsClientsTools.CurrentContext;

            if (root.UseContext != null)
            {
                if (config.DocumentsClientsTools.Contexts.ContainsKey(root.UseContext))
                    activeContextName = root.UseContext;
                else
                    throw new Exception("Configuration error, specified Context does not exist");
            }

            if (config.DocumentsClientsTools.Contexts.Any())
            {
                if (config.DocumentsClientsTools.CurrentContext != null && config.DocumentsClientsTools.Contexts.ContainsKey(activeContextName))
                    context = config.DocumentsClientsTools.Contexts[activeContextName];
                else
                    throw new Exception("Configuration error, CurrentContext not in Contexts list");
            }
            else
                context = new ConfigurationRoot.ToolsConfiguration.DocumentsAPIConfiguration
                {
                    Uri = root.Server,
                    OrganizationKey = root.OrganizationKey,
                    UserKey = root.UserKey,
                    Password = root.Password,
                    Token = root.Token
                };

            return (config.DocumentsClientsTools, context);
        }
    }
}
