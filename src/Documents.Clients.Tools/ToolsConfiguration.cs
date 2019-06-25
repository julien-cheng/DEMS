namespace Documents.Clients.Tools
{
    using System.Collections.Generic;

    public class ConfigurationRoot
    {
        public ToolsConfiguration DocumentsClientsTools { get; set; }

        public class ToolsConfiguration
        {
            public string CurrentContext { get; set; }

            public Dictionary<string, DocumentsAPIConfiguration> Contexts { get; set; }

            public class DocumentsAPIConfiguration
            {
                public string Uri { get; set; }
                public string OrganizationKey { get; set; }
                public string UserKey { get; set; }
                public string Password { get; set; }

                public string Token { get; set; }
            }
        }
    }

}
