namespace Documents.Clients.Tools.Commands.User
{
    using Documents.API.Common.Models;
    using McMaster.Extensions.CommandLineUtils;
    using System.Threading.Tasks;

    [Subcommand("get", typeof(Get))]
    [Subcommand("list", typeof(List))]
    [Subcommand("set", typeof(Set))]
    [Subcommand("delete", typeof(Delete))]
    [Subcommand("create", typeof(Create))]
    class UserCommand : CommandBase
    {
        public void RenderUser(UserModel model, bool showPrivileges = false)
        {
            if (model != null)
            {
                Table("User", new
                {
                    model.Identifier.OrganizationKey,
                    model.Identifier.UserKey,
                    model.EmailAddress,
                    UserAccessIdentifiers = model.UserAccessIdentifiers != null
                        ? string.Join(' ', model.UserAccessIdentifiers)
                        : string.Empty
                });
            }
            else
                throw new System.Exception("User does not exist");
        }

        class Get : CommandBase
        {
            [Argument(0, Description = "Key")]
            public string Key { get; }

            protected async override Task ExecuteAsync()
            {
                var UserIdentifier = GetUserIdentifier(Key);

                var model = await API.User.GetOrThrowAsync(UserIdentifier);

                GetParent<UserCommand>().RenderUser(model);
            }
        }

        class List : CommandBase
        {
            [Argument(0, Description = "Key")]
            public string Key { get; }

            protected async override Task ExecuteAsync()
            {
                var organizationIdentifier = GetOrganizationIdentifier(Key);

                var model = await API.Organization.GetOrThrowAsync(organizationIdentifier,
                    new[] { new PopulationDirective(nameof(OrganizationModel.Users)) });

                Table("Users", model.Users.Rows);
            }
        }


        class Delete : CommandBase
        {
            [Argument(0, Description = "Key")]
            public string Key { get; }

            protected async override Task ExecuteAsync()
            {
                var userIdentifier = GetUserIdentifier(Key);

                var model = await API.User.DeleteAsync(userIdentifier);

                //GetParent<UserCommand>().RenderUser(model);
            }
        }

        class Set : CommandBase
        {
            [Argument(0, Description = "Key")]
            public string Key { get; }

            [Option]
            public string Password { get; }

            [Option]
            public string AccessIdentifiers { get; }

            [Option]
            public string FirstName { get; }

            [Option]
            public string LastName { get; }

            [Option]
            public string EmailAddress { get; }

            protected async override Task ExecuteAsync()
            {
                var userIdentifier = GetUserIdentifier(Key);

                var model = await API.User.GetAsync(userIdentifier);

                if (EmailAddress != null
                    || FirstName != null
                    || LastName != null)
                {
                    model.FirstName = FirstName ?? model.FirstName;
                    model.LastName = LastName ?? model.LastName;
                    model.EmailAddress = EmailAddress ?? model.EmailAddress;
                    model = await API.User.PutAsync(model);
                }

                if (Password != null)
                    await API.User.PasswordPutAsync(userIdentifier, Password);

                if (AccessIdentifiers != null)
                    await API.User.AccessIdentifiersPutAsync(userIdentifier, AccessIdentifiers.Split(' '));


                GetParent<UserCommand>().RenderUser(model);
            }
        }

        class Create : CommandBase
        {
            [Argument(0, Description = "Key")]
            public string Key { get; }

            [Option]
            public string FirstName { get; }

            [Option]
            public string LastName { get; }

            [Option]
            public string Email { get; }

            [Option]
            public string Password { get; }

            [Option]
            public string AccessIdentifiers { get; }

            protected async override Task ExecuteAsync()
            {
                var userIdentifier = GetUserIdentifier(Key);

                var model = await API.User.GetAsync(userIdentifier);
                if (model == null)
                    model = new UserModel(userIdentifier)
                    {
                        FirstName = FirstName,
                        LastName = LastName,
                        EmailAddress = Email,
                    };
                else
                {
                    model.FirstName = FirstName ?? model.FirstName;
                    model.LastName = LastName ?? model.LastName;
                    model.EmailAddress = Email ?? model.EmailAddress;
                }
                model = await API.User.PutAsync(model);

                if (Password != null)   
                    await API.User.PasswordPutAsync(userIdentifier, Password);

                if (AccessIdentifiers != null)
                    await API.User.AccessIdentifiersPutAsync(userIdentifier, AccessIdentifiers.Split(' '));

                GetParent<UserCommand>().RenderUser(model);
            }
        }
    }
}
