namespace Documents.API.Common.Models.MetadataModels
{
    public class MetadataKeyConstants
    {
        public const string HIDDEN = "_hidden";
        public const string CHILDOF = "_childof";

        public const string ATTRIBUTE_LOCATORS = "attributeLocators[locatorList]";
        public const string ALTERNATIVE_VIEWS = "AlternativeViews";
        public const string BACKEND_CONFIGURATION = "BackendConfiguration";
        public const string SCHEMA_DEFINITION = "schema[definition]";

        public static readonly string E_DISCOVERY_PATH_METAKEY = "eDiscovery[path]";
        public static readonly string E_DISCOVERY_ACTIVE_METAKEY = "eDiscovery[isActive]";
        public static readonly string E_DISCOVERY_RECIPIENT_LIST = "eDiscovery[Recipients]";
        public static readonly string E_DISCOVERY_SHARE_STATE_META_KEY = "eDiscovery[ShareState]";
        public static readonly string E_DISCOVERY_PACKAGE_MAP_METAKEY = "eDiscovery[PackageMap]";
        public static readonly string E_DISCOVERY_SHARE_PACKGAGE = "eDiscovery[Package]";
        public static readonly string E_DISCOVERY_EXPIRATION_LENGTH_SECONDS = "eDiscovery[ExpirationSeconds]";
        public static readonly string E_DISCOVERY_RND_PASSWORD_CHARS = "eDiscovery[RandomPasswordCharSet]";
        public static readonly string E_DISCOVERY_RND_PASSWORD_LENGTH = "eDiscovery[RandomPasswordLength]";

        public static readonly string LEO_UPLOAD_ACTIVE_METAKEY = "leoUpload[isActive]";
        public static readonly string LEO_UPLOAD_OFFICERS = "leoUpload[Officers]";
        public static readonly string LEO_UPLOAD_EXPIRATION_LENGTH_SECONDS = "leoUpload[ExpirationSeconds]";
        public static readonly string LEO_UPLOAD_RND_PASSWORD_CHARS = "leoUpload[RandomPasswordCharSet]";
        public static readonly string LEO_UPLOAD_RND_PASSWORD_LENGTH = "leoUpload[RandomPasswordLength]";

        // Attribute Key Constants
        public const string ATTRIBUTE_WIDTH = "attribute.width";
        public const string ATTRIBUTE_HEIGHT = "attribute.height";
    }
}
