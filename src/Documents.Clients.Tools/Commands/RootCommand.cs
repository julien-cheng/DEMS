namespace Documents.Clients.Tools.Commands
{
    using Documents.API.Client;
    using Documents.API.Common.Models;
    using Documents.Clients.Tools.Commands.File;
    using Documents.Clients.Tools.Commands.Folder;
    using Documents.Clients.Tools.Commands.Organization;
    using Documents.Clients.Tools.Commands.Tools;
    using Documents.Clients.Tools.Commands.User;
    using McMaster.Extensions.CommandLineUtils;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Threading.Tasks;

    [Command]
    [Subcommand("organization", typeof(OrganizationCommand))]
    [Subcommand("folder", typeof(FolderCommand))]
    [Subcommand("file", typeof(FileCommand))]
    [Subcommand("context", typeof(ContextCommand))]
    [Subcommand("server", typeof(ServerCommand))]
    [Subcommand("security", typeof(SecurityCommand))]
    [Subcommand("index", typeof(IndexCommand))]
    [Subcommand("sync", typeof(SyncCommand))]
    [Subcommand("user", typeof(UserCommand))]
    [Subcommand("search", typeof(SearchCommand))]
    [Subcommand("queue", typeof(QueueCommand))]
    [Subcommand("notify", typeof(NotifyCommand))]
    [Subcommand("import", typeof(ImportCommand))]
    [Subcommand("tools", typeof(ToolsCommand))]
    [Subcommand("patrol", typeof(PatrolCommand))]
    [Subcommand("log", typeof(LogCommand))]
    public class RootCommand : CommandBase, ILogger
    {

        [Option("--context", Description = "override the default, saved context and use the mentioned one")]
        public string UseContext { get; }

        [Option(Description = "full url of the DMS API server")]
        public string Server { get; }

        [Option(Description = "specify a token for authentication")]
        public string Token { get; }

        [Option(Description = "specify a UserKey for authentication")]
        public string UserKey { get; }

        [Option(Description = "specify an OrganizationKey for authentication")]
        public string OrganizationKey { get; }

        [Option(Description = "enable api client debug logging to console")]
        public bool Logging { get; }

        [Option(Template="--impersonate-organization-key", Description = "after authentication, impersonate this organization/user")]
        public string ImpersonateOrganizationKey { get; }

        [Option(Template = "--impersonate-user-key", Description = "after authentication, impersonate this organization/user")]
        public string ImpersonateUserKey { get; }

        [Option(Description = "spefify a password for authentication")]
        public string Password { get; }

        [Option(Description = "Do not use auth token cached in context, reauthenticate instead")]
        public bool NoCachedToken { get; set; } = false;

        private Connection connection = null;
        public Connection Connection
        {
            get
            {
                if (connection == null)
                    connection = ConnectAsync().Result;

                return connection;
            }
        }

        public async Task<Connection> ConnectAsync()
        {
            (var config, var context) = Configuration.Load(this);

            if (ImpersonateOrganizationKey != null)
                NoCachedToken = true;

            if (!Uri.TryCreate(Server ?? context.Uri ?? string.Empty, UriKind.Absolute, out Uri serverUri))
                throw new Exception("Config Server URI missing/invalid");

            var connection = new Connection(serverUri);

            if (Logging)
                connection.Logger = this;

            async Task authenticate()
            {
                var tokenResponse = await connection.User.AuthenticateAsync(new UserIdentifier
                {
                    OrganizationKey = OrganizationKey ?? context.OrganizationKey,
                    UserKey = UserKey ?? context.UserKey
                }, Password ?? context.Password);

                if (!NoCachedToken)
                {
                    context.Token = tokenResponse.Token;
                    Configuration.Save(config);
                }

                if (ImpersonateOrganizationKey != null)
                    await connection.User.ImpersonateAsync(new UserIdentifier(ImpersonateOrganizationKey, ImpersonateUserKey));
            }

            if (!string.IsNullOrEmpty(context.Token) && !NoCachedToken)
            {
                connection.Token = context.Token;
                connection.OnSecurityException = authenticate;
            }
            else
            {
                if (context.OrganizationKey == null)
                    throw new Exception("Config missing OrganizationKey");
                if (context.UserKey == null)
                    throw new Exception("Config missing UserKey");
                if (context.Password == null)
                    throw new Exception("Config missing Password");

                await authenticate();
                connection.OnSecurityException = authenticate;
            }

            return connection;
        }

        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (this.Logging)
                Console.WriteLine($"[{logLevel}] {formatter(state, exception)}");
        }

        bool ILogger.IsEnabled(LogLevel logLevel)
        {
            return this.Logging;
        }

        IDisposable ILogger.BeginScope<TState>(TState state)
        {
            return new NullDisposable();
        }

        private class NullDisposable : IDisposable
        {
            void IDisposable.Dispose()
            {
            }
        }
    }
}
