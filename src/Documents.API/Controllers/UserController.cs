namespace Documents.API.Controllers
{
    using Documents.API.Authentication;
    using Documents.API.Common;
    using Documents.API.Common.Events;
    using Documents.API.Common.Models;
    using Documents.API.Events;
    using Documents.API.Exceptions;
    using Documents.API.Models;
    using Documents.Store;
    using Documents.Store.Utilities;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class UserController : ModelControllerBase<UserModel, UserIdentifier, IUserStore>
    {
        protected override EventBase EventGet(UserModel model) => new UserGetEvent();
        protected override EventBase EventPut(UserModel model) => new UserPutEvent();
        protected override EventBase EventPost(UserModel model) => new UserPostEvent();
        protected override EventBase EventDelete(UserIdentifier identifier) => new UserDeleteEvent();

        private readonly IOrganizationStore OrganizationStore;
        private readonly JWT JWT;
        private readonly ISecurityContext SecurityContext;

        public UserController(
            IUserStore store,
            IOrganizationStore organizationStore,
            JWT jwt,
            IEventSender eventSender,
            ISecurityContext securityContext
        ) : base(store, eventSender, securityContext)
        {
            this.OrganizationStore = organizationStore;
            this.JWT = jwt;
            this.SecurityContext = securityContext;
        }

        protected override IEnumerable<PopulationDirective> DefaultPopulationRelationships => new[]
        {
            new PopulationDirective
            {
                Name = nameof(FileModel.Metadata)
            }
        };

        [
            AllowAnonymous, 
            HttpPost, 
            Route("authenticate")
        ]
        public async Task<TokenResponseModel> Authenticate([FromBody] TokenRequestModel request)
        {
            var user = await Store.PasswordVerifyAsync(request.Identifier, request.Password);
            if (user != null)
            {
                await EventSender.SendAsync(new UserAuthenticated
                {
                    UserIdentifierTopic = request.Identifier
                });

                SecurityContext.AssumeUser(user);

                return new TokenResponseModel
                {
                    Token = JWT.CreateUserToken(user, request.ClientClaims),
                    User = user,
                    Organization = await OrganizationStore.GetOneAsync(request.Identifier)
                };
            }
            else
                throw new SecurityException();
        }

        [
            HttpPost,
            Route("impersonate")
        ]
        public async Task<TokenResponseModel> Impersonate([FromBody] UserIdentifier userIdentifier)
        {
            if (userIdentifier == null)
                throw new ArgumentNullException(nameof(userIdentifier));

            var organizationModel = await OrganizationStore.GetOneAsync(userIdentifier);
            var userModel = await Store.GetOneAsync(userIdentifier);

            if (organizationModel == null || userModel == null)
                throw new ObjectDoesNotExistException();

            organizationModel.PrivilegeCheck("user:impersonate", SecurityContext);

            if (userModel != null)
            {
                await EventSender.SendAsync(new UserImpersonated
                {
                    UserIdentifierTopic = userIdentifier
                });

                return new TokenResponseModel
                {
                    Token = JWT.CreateUserToken(userModel),
                    User = userModel,
                    Organization = organizationModel
                };
            }
            else
                throw new SecurityException();
        }


        [HttpPut, Route("password")]
        public async Task<UserModel> PasswordPut([FromBody] UserPasswordPutRequest request)
        {
            return await Store.PasswordSetAsync(request.Identifier, request.Password);
        }

        [HttpPut, Route("accessidentifiers")]
        public async Task<UserModel> IdentifiersPut([FromBody] UserAccessIdentifiersPutRequest request)
        {
            return await Store.AccessIdentifiersSetAsync(request.Identifier, request.AccessIdentifiers);
        }
    }
}
