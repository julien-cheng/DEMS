
using System.Linq;

namespace Documents.Clients.Manager.Modules.LEOUpload
{
    public static class LEOUploadUtility
    {
        public static readonly string LEO_UPLOAD_PATH_NAME = "LEO Upload";
        public static readonly string LEO_UPLOAD_PATH_KEY = "LEO Upload";

        public static readonly string LEO_UPLOAD_DEFAULT_PASSWORD_LENGTH = "8";

        public static readonly string LEO_UPLOAD_FOLDER_COLOR_STYLE = "folder blue";

        public static bool IsUserLeo(string[] userAccessIdentifiers)
        {
            return userAccessIdentifiers?.Any(i => i == "r:leoUpload") ?? false;
        }
    }
}
