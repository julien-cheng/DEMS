namespace Documents.API.Common
{
    using Documents.API.Common.Models;

    public interface ISecurityContext
    {
        bool IsAuthenticated { get; }
        UserIdentifier UserIdentifier { get; }
        string[] SecurityIdentifiers { get; }

        void AssumeUser(UserModel user);
        void AssumeToken(string token);
    }
}
