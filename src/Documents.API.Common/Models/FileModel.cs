namespace Documents.API.Common.Models
{
    using Documents.API.Common.MetadataPersistence;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class FileModel : PrivilegeModelBase, IProvideMetadata, IProvidePrivileges, IHasIdentifier<FileIdentifier>, IProvideETag
    {
        public FileModel() { }
        public FileModel(FileIdentifier identifier)
        {
            this.Identifier = identifier;
        }

        public FileIdentifier Identifier { get; set;}

        public string Name { get; set; }

        [JsonIgnore]
        public string Extension { get => this.Name?.ToLower().Split('.').LastOrDefault() ?? string.Empty; }

        public string MimeType { get; set; }
        public long Length { get; set; }
        public string LengthForHumans
        {
            get
            {
                string[] sizes = { "B", "KB", "MB", "GB", "TB" };
                double len = Length;
                int order = 0;
                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len = len / 1024;
                }

                // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
                // show a single decimal place, and no space.
                return String.Format("{0:0.##} {1}", len, sizes[order]);
            }
        }

        public string HashMD5 { get; set; }
        public string HashSHA1 { get; set; }
        public string HashSHA256 { get; set; }

        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }

        public IDictionary<string, IDictionary<string, string>> FileMetadata { get; set; }
        public IDictionary<string, IDictionary<string, IEnumerable<ACLModel>>> FilePrivileges { get; set; }

        public string NameWithoutExtension()
        {
            var ext = this.Extension;
            if (ext != string.Empty)
                return this.Name.Substring(0, this.Name.Length - ext.Length - 1);
            else
                return this.Name;
        }

        string IProvideETag.ETag { get; set; }

        public static string Tier => "file";
        private static readonly string[] TierSearchOrder = new[] { FileModel.Tier, FolderModel.Tier, OrganizationModel.Tier };

        public string Metadata
            (string key) => FileMetadata.Search(key, TierSearchOrder);

        [JsonIgnore]
        public IDictionary<string, string> MetadataFlattened
            => FileMetadata.Flatten(TierSearchOrder);

        public override IEnumerable<ACLModel> Privilege
            (string key) => FilePrivileges.Search(key, TierSearchOrder);

        [JsonIgnore]
        public IDictionary<string, IEnumerable<ACLModel>> PrivilegesFlattened
            => FilePrivileges.Flatten(TierSearchOrder);

        public FileModel InitializeEmptyMetadata()
        {
            this.FileMetadata = new Dictionary<string, IDictionary<string, string>>();

            return this;
        }

        public FileModel InitializeEmptyPrivileges()
        {
            this.FilePrivileges = new Dictionary<string, IDictionary<string, IEnumerable<ACLModel>>>();

            return this;
        }

        void IProvideMetadata.Write(string key, object value, bool withTypeName)
        {
            this.Write<object>(key, value, withTypeName);
        }

        public enum OnlineStatus
        {
            Offline,
            Restoring,
            Online
        }
    }
}
