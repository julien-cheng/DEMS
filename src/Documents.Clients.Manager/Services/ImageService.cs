namespace Documents.Clients.Manager.Services
{
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;
    using Documents.Clients.Manager.Common;
    using Documents.Clients.Manager.Models;
    using Documents.Clients.Manager.Models.ViewSets;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class ImageService
    {
        protected readonly APIConnection Connection;

        private const string MIME_PNG = "image/png";
        private const string MIME_JPG = "image/jpeg";
        private const string MIME_GIF = "image/gif";

        public ImageService(APIConnection connection)
        {
            Connection = connection;
        }

        public async Task<ImageSet> ImageSetGetAsync(FileIdentifier fileIdentifier)
        {
            var file = await Connection.File.GetAsync(fileIdentifier);
            return ImageSetGet(file);
        }

        public static ImageSet ImageSetGet(FileModel file)
        {
            return ImageSetGet(file.Identifier, file.Read<List<AlternativeView>>(MetadataKeyConstants.ALTERNATIVE_VIEWS), file.Extension, file.MimeType);
        }

        public static ImageSet ImageSetGet(FileIdentifier fileIdentifier, List<AlternativeView> alternativeViews, string extension, string mimeType)
        {
            var imageSet = new ImageSet
            {
                ImageType = ImageSet.ImageTypeEnum.Unknown
            };

            if (IsImageExtension(extension) || IsImageMimeType(mimeType))
                imageSet.FileIdentifier = fileIdentifier;

            if (imageSet.FileIdentifier != null)
                imageSet.ImageType = ImageSet.ImageTypeEnum.Image;

            var exif = alternativeViews?.FirstOrDefault(v => v.Name == "EXIF")?.FileIdentifier;

            imageSet.PreviewImageIdentifier = alternativeViews
                ?.FirstOrDefault(v => 
                    v.SizeType == "Thumbnail" 
                    || (v.SizeType == null
                        && v.ImageFormat == ImageFormatEnum.PNG 
                        && v.Height == 100) // there was a period where SizeType was missing
                )
                ?.FileIdentifier;

            var allowedOperations = new List<AllowedOperation>
            {
                AllowedOperation.GetAllowedOperationDownload(fileIdentifier, label: "Download")
            };

            if (exif != null)
                allowedOperations.Add(AllowedOperation.GetAllowedOperationDownload(exif, label: "Download EXIF"));

            imageSet.AllowedOperations = allowedOperations;

            return imageSet;
        }

        private static bool IsImageMimeType(string mimeType)
        {
            switch (mimeType)
            {
                case MIME_PNG:
                case MIME_GIF:
                case MIME_JPG:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsImageExtension(string extension)
        {
            switch (extension)
            {
                case "jpg":
                case "jpeg":
                case "gif":
                case "png":
                    return true;
                default:
                    return false;
            }
        }
    }
}
