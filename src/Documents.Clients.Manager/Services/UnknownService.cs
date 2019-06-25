namespace Documents.Clients.Manager.Services
{
    using Documents.API.Common.EventHooks;
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;
    using Documents.Clients.Manager.Common;
    using Documents.Clients.Manager.Models;
    using Documents.Clients.Manager.Models.ViewSets;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class UnknownService
    {
        protected readonly APIConnection Connection;

        public UnknownService(APIConnection connection)
        {
            Connection = connection;
        }

        public async Task<UnknownSet> UnknownSetGetAsync(FileIdentifier fileIdentifier)
        {
            var file = await Connection.File.GetAsync(fileIdentifier);
            return UnknownSetGet(file);
        }

        public static UnknownSet UnknownSetGet(FileModel file)
        {
            var unknownSet = UnknownSetGet(file.Identifier, file.Read<List<AlternativeView>>(MetadataKeyConstants.ALTERNATIVE_VIEWS), file.Extension, file.MimeType);

            var actions = FileHandling.GetFileActions(file);

            if (actions.ConvertToPDF)
                unknownSet.Message = new UnknownSet.MessageDetails
                {
                    Title = "File not ready",
                    Body = "This file appears to be a document. The server is " +
                        "attempting to make it viewable online, but it is not yet ready. " +
                        "Please check back later."
                };
            else if (actions.Transcode || actions.TranscodeAudio)
                unknownSet.Message = new UnknownSet.MessageDetails
                {
                    Title = "File not ready",
                    Body = "This file appears to contain audio or video. " +
                        "The server is attempting to make it playable online. " +
                        "Please check back later."
                };
            else
                unknownSet.Message = new UnknownSet.MessageDetails
                {
                    Title = "File is unknown",
                    Body = "This file type is not playable or viewable online. " +
                        "You may download it only."
                };

            return unknownSet;
        }

        public static UnknownSet UnknownSetGet(FileIdentifier fileIdentifier, List<AlternativeView> alternativeViews, string extension, string mimeType)
        {

            var unknownSet = new UnknownSet
            {
                FileIdentifier = fileIdentifier
            };

            var allowedOperations = new List<AllowedOperation>
            {
                AllowedOperation.GetAllowedOperationDownload(fileIdentifier, label: "Download")
            };
            
            unknownSet.AllowedOperations = allowedOperations;

            return unknownSet;
        }
    }
}
