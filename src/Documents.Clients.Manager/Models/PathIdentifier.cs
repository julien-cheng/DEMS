namespace Documents.Clients.Manager.Models
{
    using Documents.API.Common.Models;
    using Newtonsoft.Json;
    using NSwag.Annotations;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class PathIdentifier : FolderIdentifier, IIdentifier
    {
        public PathIdentifier() { }
        public PathIdentifier(FolderIdentifier parentIdentifier, string pathKey = null)
            : this(parentIdentifier.OrganizationKey, parentIdentifier.FolderKey, pathKey)
        { }

        public PathIdentifier(string organizationKey, string folderKey, string pathKey = null, bool dontThrow = false)
        {
            this.OrganizationKey = organizationKey ?? throw new System.ArgumentNullException(nameof(organizationKey));
            this.FolderKey = folderKey ?? throw new System.ArgumentNullException(nameof(folderKey));

            try
            {
                this.PathKey = pathKey;
            }
            catch (Exception)
            {
                if (!dontThrow)
                    throw;
            }
        }

        private string[] Parts = new string[0];

        private string _PathKey = null;
        public string PathKey
        {
            get
            {
                return _PathKey;
            }
            set
            {
                _PathKey = value ?? string.Empty;
                Parts = _PathKey.Split('/');

                // disallow starting or ending with a slash
                // disallow empty internal paths (like a//b) as well
                if (Parts.Length > 1 && Parts.Any(p => string.IsNullOrEmpty(p)))
                    throw new Exception($"Malformed Path '{value}'");
            }
        }

        [JsonIgnore, SwaggerIgnore]
        string IIdentifier.ComponentKey { get => PathKey; set => PathKey = value; }

        [JsonIgnore, SwaggerIgnore]
        public override bool IsValid
            => OrganizationKey != null
            && FolderKey != null
            && PathKey != null;

        public override string ToString()
        {
            return $"{{pat:{PathKey},fol:{FolderKey},org:{OrganizationKey}}}";
        }

        [JsonIgnore, SwaggerIgnore]
        public string FullName
            => PathKey;

        [JsonIgnore, SwaggerIgnore]
        public string LeafName
        {
            get
            {
                return Parts.Count() > 0 ? Parts.Last() : "";
            }
        }

            

        [JsonIgnore, SwaggerIgnore]
        public bool IsRoot
        {
            get
            {
                return this.ParentPathIdentifier == null;
            }
        }

        [JsonIgnore, SwaggerIgnore]
        public PathIdentifier ParentPathIdentifier
        {
            get
            {
                // if this is the root path, there is no parent
                if (Parts.Length == 1 && Parts[0] == string.Empty)
                    return null;

                if (!this.IsValid)
                    return null;

                // root is empty string, not null
                var parentPathKey = string.Join("/", Parts.Take(Parts.Length - 1));
                return new PathIdentifier(this as FolderIdentifier, parentPathKey);
            }
        }

        [JsonIgnore, SwaggerIgnore]
        public IEnumerable<PathIdentifier> ParentPathIdentifiers
        {
            get
            {
                var path = this.ParentPathIdentifier;
                while (path != null)
                {
                    yield return path;
                    path = path.ParentPathIdentifier;
                }
            }
        }

        public PathIdentifier RelativeTo(PathIdentifier parentPathIdentifier)
        {
            if (this.IsChildOf(parentPathIdentifier)
                && parentPathIdentifier?.PathKey != null
                && PathKey != null
                && !this.Equals(parentPathIdentifier))
            {
                var trimChars = 0;

                if (parentPathIdentifier.PathKey.Length > 0)
                    trimChars = parentPathIdentifier.PathKey.Length + 1;

                return new PathIdentifier(new FolderIdentifier(parentPathIdentifier, parentPathIdentifier.FolderKey),
                    _PathKey.Substring(trimChars));
            }
            else
                return null;
        }

        public PathIdentifier CreateChild(string name)
        {
            var newPathKey = string.IsNullOrEmpty(this.PathKey)
                ? name
                : this.PathKey + '/' + name;

            return new PathIdentifier(this as FolderIdentifier, newPathKey);
        }

        public override bool Equals(object obj)
        {
            PathIdentifier b = obj as PathIdentifier;
            if (b is null)
                return false;

            return
                b.OrganizationKey == OrganizationKey
                && b.FolderKey == FolderKey
                && b.PathKey == PathKey;
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        public static PathIdentifier Root(FolderIdentifier folderIdentifier)
        {
            return new PathIdentifier(folderIdentifier, string.Empty);
        }

        public bool IsChildOf(PathIdentifier parent)
        {
            if (parent == null)
                return false;

            var pathKey = this.PathKey ?? string.Empty;
            var parentPathKey = parent.PathKey ?? string.Empty;

            // if parent is root
            if (parentPathKey == string.Empty)
                return true;

            // if parent == this
            if (pathKey.Equals(parentPathKey))
                return true;

            // if this is a child of parent
            if (pathKey.StartsWith(parentPathKey + '/'))
                return true;

            return false;
        }
    }
}
