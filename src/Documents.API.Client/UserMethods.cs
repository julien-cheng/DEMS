namespace Documents.API.Client
{
    using Documents.API.Common.Models;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    public class UserMethods : RESTBase<UserModel, UserIdentifier>
    {
        public UserMethods(Connection connection)
            : base(connection, APIEndpoint.User)
        { }

        public Task<TokenResponseModel> AuthenticateAsync(
            UserIdentifier userIdentifier,
            string password,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => AuthenticateAsync(new TokenRequestModel { Identifier = userIdentifier, Password = password });

        public async Task<TokenResponseModel> AuthenticateAsync(
            TokenRequestModel tokenRequest,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            var tokenResponse = await Connection.APICallAsync<TokenResponseModel>(HttpMethod.Post, APIEndpoint.UserAuthenticate,
                bodyContent: tokenRequest,
                cancellationToken: cancellationToken);

            Connection.Token = tokenResponse.Token;

            return tokenResponse;
        }

        public async Task<TokenResponseModel> ImpersonateAsync(
            UserIdentifier userIdentifier,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            var tokenResponse = await Connection.APICallAsync<TokenResponseModel>(HttpMethod.Post, APIEndpoint.UserImpersonate,
                bodyContent: userIdentifier,
                cancellationToken: cancellationToken);

            Connection.Token = tokenResponse.Token;

            return tokenResponse;
        }

        public async Task<UserModel> PasswordPutAsync(
            UserIdentifier identifier,
            string password,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            var user = await Connection.APICallAsync<UserModel>(HttpMethod.Put, APIEndpoint.UserPassword,
                bodyContent: new
                {
                    identifier,
                    password
                },
                cancellationToken: cancellationToken);

            return user;
        }

        public async Task<UserModel> AccessIdentifiersPutAsync(
            UserIdentifier identifier,
            IEnumerable<string> accessIdentifiers,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            var user = await Connection.APICallAsync<UserModel>(HttpMethod.Put, APIEndpoint.UserAccessIdentifiers,
                bodyContent: new
                {
                    identifier,
                    accessIdentifiers
                },
                cancellationToken: cancellationToken);

            return user;
        }
    }
}
