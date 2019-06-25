namespace Documents.API.Common.Models
{
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using Documents.API.Common.MetadataPersistence;

    public class FolderModel : PrivilegeModelBase, IProvideMetadata, IProvidePrivileges, IHasIdentifier<FolderIdentifier>, IProvideETag
    {
        public FolderModel() { }
        public FolderModel(FolderIdentifier identifier)
        {
            this.Identifier = identifier;
        }

        public FolderIdentifier Identifier { get; set; }

        public PagedResults<FileModel> Files { get; set; }

        public IDictionary<string, IDictionary<string, string>> FileMetadata { get; set; }
        public IDictionary<string, IDictionary<string, string>> FolderMetadata { get; set; }

        public IDictionary<string, IDictionary<string, IEnumerable<ACLModel>>> FilePrivileges { get; set; }
        public IDictionary<string, IDictionary<string, IEnumerable<ACLModel>>> FolderPrivileges { get; set; }

        string IProvideETag.ETag { get; set; }

        public static string Tier => "folder";
        private static readonly string[] TierSearchOrder = new[] { FolderModel.Tier, OrganizationModel.Tier };

        public string Metadata(string key) 
            => FolderMetadata.Search(key, TierSearchOrder);

        [JsonIgnore]
        public IDictionary<string, string> MetadataFlattened
            => FolderMetadata.Flatten(TierSearchOrder);

        public override IEnumerable<ACLModel> Privilege
            (string key) => FolderPrivileges.Search(key, TierSearchOrder);

        [JsonIgnore]
        public IDictionary<string, IEnumerable<ACLModel>> PrivilegesFlattened
            => FolderPrivileges.Flatten(TierSearchOrder);

        public FolderModel InitializeEmptyMetadata()
        {
            this.FolderMetadata = new Dictionary<string, IDictionary<string, string>>();
            this.FileMetadata = new Dictionary<string, IDictionary<string, string>>();

            return this;
        }

        public FolderModel InitializeEmptyPrivileges()
        {
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
