namespace Documents.Store
{
    using Documents.API.Common.Models;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IUserStore : IModelStore<UserModel, UserIdentifier>
    {
        Task<UserModel> PasswordVerifyAsync(UserIdentifier identifier, string password);
        Task<UserModel> PasswordSetAsync(UserIdentifier identifier, string password);
        Task<UserModel> AccessIdentifiersSetAsync(UserIdentifier identifier, IEnumerable<string> userAcessIdentifiers);
    }
}