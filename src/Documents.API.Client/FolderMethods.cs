namespace Documents.API.Client
{
    using Documents.API.Common.Models;

    public class FolderMethods : RESTBase<FolderModel, FolderIdentifier>
    {
        public FolderMethods(Connection connection)
            : base(connection, APIEndpoint.Folder)
        { }
    }

}
