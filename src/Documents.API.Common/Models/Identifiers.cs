namespace Documents.API.Common.Models
{
    using Newtonsoft.Json;
    using System;

    public interface IHasIdentifier<TIdentifier>
        where TIdentifier: IIdentifier
    {
        TIdentifier Identifier { get; set; }
    }
    public interface IIdentifier
    {
        [JsonIgnore]
        string ComponentKey { get; set; }

        [JsonIgnore]
        bool IsValid { get; }
    }

    public abstract class Identifier : IIdentifier
    {
        string IIdentifier.ComponentKey { get; set; }
        public abstract bool IsValid { get; }
    }
    public class OrganizationIdentifier : Identifier, IIdentifier
    {
        public OrganizationIdentifier() { }
        public OrganizationIdentifier(string organizationKey)
        {
            this.OrganizationKey = organizationKey;
        }

        public string OrganizationKey { get; set; }

        string IIdentifier.ComponentKey { get => OrganizationKey; set => OrganizationKey = value;}
        public override bool IsValid
            => OrganizationKey != null;

        public override string ToString()
        {
            return $"[org:{OrganizationKey}]";
        }

        public override bool Equals(object obj)
        {
            var other = obj as OrganizationIdentifier;
            if (other == null)
                return false;

            return other.OrganizationKey == OrganizationKey;
        }

        public override int GetHashCode()
        {
            return new { OrganizationKey }.GetHashCode();
        }

    }

    public class FolderIdentifier : OrganizationIdentifier, IIdentifier
    {
        public FolderIdentifier() { }
        public FolderIdentifier(OrganizationIdentifier parentIdentifier, string folderKey = null)
            : this(parentIdentifier.OrganizationKey, folderKey) {}
        public FolderIdentifier(string organizationKey, string folderKey = null)
        {
            this.OrganizationKey = organizationKey ?? throw new System.ArgumentNullException(nameof(organizationKey));
            this.FolderKey = folderKey;
        }

        public string FolderKey { get; set; }

        string IIdentifier.ComponentKey { get => FolderKey; set => FolderKey = value; }
        public override bool IsValid
            => OrganizationKey != null
            && FolderKey != null;

        public override string ToString()
        {
            return $"{{fol:{FolderKey},org:{OrganizationKey}}}";
        }

        public override bool Equals(object obj)
        {
            var other = obj as FolderIdentifier;
            if (other == null)
                return false;

            return other.FolderKey == FolderKey
                && other.OrganizationKey == OrganizationKey;
        }

        public override int GetHashCode()
        {
            return new { OrganizationKey, FolderKey }.GetHashCode();
        }
    }

    public class FileIdentifier : FolderIdentifier, IIdentifier
    {
        public FileIdentifier() { }
        public FileIdentifier(FolderIdentifier parentIdentifier, string fileKey = null)
            : this(parentIdentifier.OrganizationKey, parentIdentifier.FolderKey, fileKey)
        {}

        public FileIdentifier(string organizationKey, string folderKey, string fileKey = null)
        {
            this.OrganizationKey = organizationKey ?? throw new System.ArgumentNullException(nameof(organizationKey));
            this.FolderKey = folderKey ?? throw new System.ArgumentNullException(nameof(folderKey));
            this.FileKey = fileKey;
        }

        public string FileKey { get; set; }

        string IIdentifier.ComponentKey { get => FileKey; set => FileKey = value; }
        public override bool IsValid
            => OrganizationKey != null
            && FolderKey != null
            && FileKey != null;

        public override string ToString()
        {
            return $"{{fil:{FileKey},fol:{FolderKey},org:{OrganizationKey}}}";
        }

        public override bool Equals(object obj)
        {
            var other = obj as FileIdentifier;
            if (other == null)
                return false;

            return other.FileKey == FileKey
                && other.FolderKey == FolderKey
                && other.OrganizationKey == OrganizationKey;
        }

        public override int GetHashCode()
        {
            return new { OrganizationKey, FolderKey, FileKey }.GetHashCode();
        }
    }

    public class UserIdentifier : OrganizationIdentifier, IIdentifier
    {
        public UserIdentifier() { }
        public UserIdentifier(OrganizationIdentifier parentIdentifier, string userKey = null)
            : this(parentIdentifier.OrganizationKey, userKey)
        { }

        public UserIdentifier(string organizationKey, string userKey = null)
        {
            this.OrganizationKey = organizationKey ?? throw new System.ArgumentNullException(nameof(organizationKey));
            this.UserKey = userKey;
        }

        public string UserKey { get; set; }

        string IIdentifier.ComponentKey { get => UserKey; set => UserKey = value; }
        public override bool IsValid
            => OrganizationKey != null
            && UserKey != null;

        public override string ToString()
        {
            return $"{{usr:{UserKey},org:{OrganizationKey}}}";
        }

        public override bool Equals(object obj)
        {
            var other = obj as UserIdentifier;
            if (other == null)
                return false;

            return other.UserKey == UserKey
                && other.OrganizationKey == OrganizationKey;
        }

        public override int GetHashCode()
        {
            return new { OrganizationKey, UserKey }.GetHashCode();
        }
    }

    public class UploadIdentifier : FileIdentifier, IIdentifier
    {
        public UploadIdentifier() { }
        public UploadIdentifier(string organizationKey, string folderKey, string fileKey, string uploadKey = null)
            :base(organizationKey, folderKey, fileKey)
        {
            this.UploadKey = uploadKey;
        }

        public string UploadKey { get; set; }

        string IIdentifier.ComponentKey { get => UploadKey; set => UploadKey = value; }
        public override bool IsValid
            => OrganizationKey != null
            && FolderKey != null
            && FileKey != null
            && UploadKey != null;
    }

    public class UploadChunkIdentifier : UploadIdentifier, IIdentifier
    {
        public UploadChunkIdentifier() { }
        public UploadChunkIdentifier(string organizationKey, string folderKey, string fileKey, string uploadKey, string uploadChunkKey = null)
            :base(organizationKey, folderKey, fileKey, uploadKey)
        {
            this.UploadChunkKey = uploadChunkKey;
        }
        public UploadChunkIdentifier(UploadIdentifier parentIdentifier, string uploadChunkKey = null)
            :this(parentIdentifier.OrganizationKey, parentIdentifier.FolderKey, parentIdentifier.FileKey, parentIdentifier.UploadKey, uploadChunkKey)
        {}

        public string UploadChunkKey { get; set; }

        string IIdentifier.ComponentKey { get => UploadChunkKey; set => UploadChunkKey = value; }

        public override bool IsValid
            => OrganizationKey != null
            && FolderKey != null
            && FileKey != null
            && UploadKey != null
            && UploadChunkKey != null;
    }

    public class AuditLogEntryIdentifier : OrganizationIdentifier, IIdentifier
    {
        public AuditLogEntryIdentifier() { }
        public AuditLogEntryIdentifier(string organizationKey, long? auditLogID = null)
            : base(organizationKey)
        {
            this.AuditLogID = auditLogID;
        }
        public AuditLogEntryIdentifier(OrganizationIdentifier parentIdentifier, long? auditLogID = null)
            : this(parentIdentifier.OrganizationKey, auditLogID)
        { }

        public long? AuditLogID { get; set; }

        string IIdentifier.ComponentKey { get => AuditLogID?.ToString(); set => throw new NotImplementedException(); }
        public override bool IsValid
            => OrganizationKey != null
            && AuditLogID != null;
    }

}
