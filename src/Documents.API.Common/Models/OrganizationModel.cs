namespace Documents.API.Common.Models
{
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using Documents.API.Common.MetadataPersistence;

    public class OrganizationModel : PrivilegeModelBase, IProvideMetadata, IProvidePrivileges, IHasIdentifier<OrganizationIdentifier>, IProvideETag
    {
        public OrganizationIdentifier Identifier { get; set; }

        public string Name { get; set; }

        public PagedResults<FolderModel> Folders { get; set; }
        public PagedResults<UserModel> Users { get; set; }

        // <tier, <key, value>>
        public IDictionary<string, IDictionary<string, string>> FileMetadata { get; set; }
        public IDictionary<string, IDictionary<string, string>> FolderMetadata { get; set; }
        public IDictionary<string, IDictionary<string, string>> OrganizationMetadata { get; set; }

        // <tier, <right, <overrideKey, value>>>
        public IDictionary<string, IDictionary<string, IEnumerable<ACLModel>>> FilePrivileges { get; set; }
        public IDictionary<string, IDictionary<string, IEnumerable<ACLModel>>> FolderPrivileges { get; set; }
        public IDictionary<string, IDictionary<string, IEnumerable<ACLModel>>> OrganizationPrivileges { get; set; }

        string IProvideETag.ETag { get; set; }

        public OrganizationModel() { }
        public OrganizationModel(OrganizationIdentifier identifier)
        {
            this.Identifier = identifier;
        }

        public static string Tier => "organization";
        private static readonly string[] TierSearchOrder = new[] { OrganizationModel.Tier };

        public string Metadata
            (string key) => OrganizationMetadata.Search(key, TierSearchOrder);

        [JsonIgnore]
        public IDictionary<string, string> MetadataFlattened
            => OrganizationMetadata.Flatten(TierSearchOrder);

        public override IEnumerable<ACLModel> Privilege
            (string key) => OrganizationPrivileges.Search(key, TierSearchOrder);

        [JsonIgnore]
        public IDictionary<string, IEnumerable<ACLModel>> PrivilegesFlattened
            => OrganizationPrivileges.Flatten(TierSearchOrder);

        public OrganizationModel InitializeEmptyMetadata()
        {
            this.OrganizationMetadata = new Dictionary<string, IDictionary<string, string>>();
            this.FolderMetadata = new Dictionary<string, IDictionary<string, string>>();
            this.FileMetadata = new Dictionary<string, IDictionary<string, string>>();

            return this;
        }

        public OrganizationModel InitializeEmptyPrivileges()
        {
            this.OrganizationPrivileges = new Dictionary<string, IDictionary<string, IEnumerable<ACLModel>>>();
            this.FolderPrivileges = new Dictionary<string, IDictionary<string, IEnumerable<ACLModel>>>();
            this.FilePrivileges = new Dictionary<string, IDictionary<string, IEnumerable<ACLModel>>>();

            return this;
        }

        void IProvideMetadata.Write(string key, object value, bool withTypeName)
        {
            this.Write<object>(key, value, withTypeName);
        }
    }
}
