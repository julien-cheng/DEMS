namespace Documents.Clients.Manager.Services
{
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;
    using Documents.Clients.Manager.Common;
    using Documents.Clients.Manager.Models;
    using Documents.Clients.Manager.Models.ViewSets;
    using Documents.Clients.Manager.Modules.eDiscovery;
    using Documents.Clients.Manager.Modules.LEOUpload;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class ViewSetService
    {
        private readonly APIConnection api;

        public ViewSetService(APIConnection api)
        {
            this.api = api;
        }

        public async Task<T> LoadSet<T>(FileIdentifier fileIdentifier)
            where T : BaseSet
        {
            var file = await api.File.GetAsync(fileIdentifier);
            var fileStatus = await api.File.GetOnlineStatusAsync(fileIdentifier);


            var organization = await api.Organization.GetAsync(fileIdentifier as OrganizationIdentifier);
            BaseSet set = null;

            // this is awful, but no clear security check to perform
            bool isPrivileged = !EDiscoveryUtility.IsUserEDiscovery(this.api.UserAccessIdentifiers)
                && !LEOUploadUtility.IsUserLeo(this.api.UserAccessIdentifiers);

            if (typeof(T).Equals(typeof(TextSet)))
                set = TextService.TextSetGet(file, true);

            if (typeof(T).Equals(typeof(DocumentSet)))
                set = DocumentSetService.DocumentSetGet(file);

            if (typeof(T).Equals(typeof(MediaSet)))
                set = MediaService.MediaSetGet(organization, file, isPrivileged);

            if (typeof(T).Equals(typeof(TranscriptSet)))
            {
                var transcriptSet = TranscriptService.TranscriptSetGet(organization, file);
                transcriptSet.Segments = await TranscriptService.LoadSegments(api, transcriptSet.Subtitles?.FirstOrDefault()?.FileIdentifier);
                set = transcriptSet;
            }

            if (typeof(T).Equals(typeof(ClipSet)))
            {
                var clipSet = ClipService.ClipSetGet(organization, file);
                set = clipSet;
            }

            if (typeof(T).Equals(typeof(ImageSet)))
                set = ImageService.ImageSetGet(file);

            if (typeof(T).Equals(typeof(UnknownSet)))
                set = UnknownService.UnknownSetGet(file);

            if (set.AllowedOperations == null)
                set.AllowedOperations = new[]
                {
                    AllowedOperation.GetAllowedOperationDownload(fileIdentifier, false)
                };

            set.RootFileIdentifier = file.Read(MetadataKeyConstants.CHILDOF, defaultValue: file.Identifier);

            if (set.RootFileIdentifier.Equals(fileIdentifier))
                set.Views = DetectFileViews(organization, file);
            else
                set.Views = DetectFileViews(organization, await api.File.GetAsync(set.RootFileIdentifier));


            // some wierd logic for eDiscovery
            // if eDiscovery, then no subtitles
            if (set is MediaSet && EDiscoveryUtility.IsUserEDiscovery(api.UserAccessIdentifiers))
            {
                var media = set as MediaSet;
                media.Subtitles = null;
            }


            if (fileStatus != FileModel.OnlineStatus.Online)
            {
                set.Views = new ManagerFileView[] { new ManagerFileView {
                    ViewerType = ManagerFileView.ViewerTypeEnum.Offline,
                    Identifier = set.RootFileIdentifier
                }};

                set.AllowedOperations = new AllowedOperation[]
                {
                    AllowedOperation.GetAllowedOperationRequestOnlineFolder(set.RootFileIdentifier)
                };

                set.RootFileIdentifier = null;
            }

            return set as T;
        }

        public static IEnumerable<ManagerFileView> DetectFileViews(OrganizationModel organization, FileModel file)
        {
            return DetectFileViews(organization, file.Identifier, file.Read<List<AlternativeView>>(MetadataKeyConstants.ALTERNATIVE_VIEWS), file.Extension, file.MimeType);
        }

        public static IEnumerable<ManagerFileView> DetectFileViews(OrganizationModel organization, FileIdentifier fileIdentifier, List<AlternativeView> alternativeViews, string extension, string mimeType)
        {
            var views = new List<ManagerFileView>();

            var mediaSet = MediaService.MediaSetGet(organization, fileIdentifier, alternativeViews, extension, mimeType);
            if (mediaSet.MediaType != MediaSet.MediaTypeEnum.Unknown)
            {
                var view = new ManagerFileView
                {
                    Identifier = fileIdentifier,
                    ViewerType =
                        mediaSet.MediaType == MediaSet.MediaTypeEnum.Audio ? ManagerFileView.ViewerTypeEnum.Audio
                        : mediaSet.MediaType == MediaSet.MediaTypeEnum.Video ? ManagerFileView.ViewerTypeEnum.Video
                        : ManagerFileView.ViewerTypeEnum.Unknown,
                };

                if (view.ViewerType == ManagerFileView.ViewerTypeEnum.Audio)
                    view.Icons = new[] { "audio" };
                if (view.ViewerType == ManagerFileView.ViewerTypeEnum.Video)
                    view.Icons = new[] { "video" };

                view.Label = view.ViewerType == ManagerFileView.ViewerTypeEnum.Audio ? "Audio" : "Video";

                views.Add(view);

                var clipSet = ClipService.ClipSetGet(organization, fileIdentifier, alternativeViews, extension, mimeType);
                if (clipSet.MediaType != MediaSet.MediaTypeEnum.Unknown)
                {
                    view = new ManagerFileView
                    {
                        Identifier = fileIdentifier,
                        ViewerType = ManagerFileView.ViewerTypeEnum.Clip
                    };
                    view.Icons = new[] { "fa-film" };

                    view.Label = "Clip Creator";

                    views.Add(view);
                }
            }

            var imageSet = ImageService.ImageSetGet(fileIdentifier, alternativeViews, extension, mimeType);
            if (imageSet.ImageType != ImageSet.ImageTypeEnum.Unknown)
            {
                var view = new ManagerFileView
                {
                    Identifier = fileIdentifier,
                    ViewerType = ManagerFileView.ViewerTypeEnum.Image,
                    Icons = new[] { "image" }
                };

                view.Label = "Image";

                views.Add(view);
            }

            var documentSet = DocumentSetService.DocumentSetGet(fileIdentifier, alternativeViews, extension, mimeType);
            if (documentSet.DocumentType != DocumentSet.DocumentTypeEnum.Unknown)
            {
                var view = new ManagerFileView
                {
                    Identifier = fileIdentifier,
                    ViewerType = ManagerFileView.ViewerTypeEnum.Document,
                    Icons = new[] { "file" }
                };

                view.Label = "Document";

                views.Add(view);
            }

            /* suppressed as per github #144
            var exifSet = TextService.TextSetGet(fileIdentifier, alternativeViews, extension, mimeType, "EXIF");
            if (exifSet.TextType != TextSet.TextTypeEnum.Unknown)
            {
                var view = new ManagerFileView
                {
                    Identifier = exifSet.FileIdentifier,
                    ViewerType = ManagerFileView.ViewerTypeEnum.Text,
                    Icons = new[] { "fa-stethoscope" }
                };

                view.Label = "EXIF Data";
                views.Add(view);
            }*/

            var vttSet = TextService.TextSetGet(fileIdentifier, alternativeViews, extension, mimeType, 
                n => n.Name == "Voicebase WebVTT");

            if (vttSet.TextType != TextSet.TextTypeEnum.Unknown)
            {

                var editedVTTSet = TextService.TextSetGet(fileIdentifier, alternativeViews, extension, mimeType, 
                    n => n.Name == TranscriptService.FILENAME_VTT);

                /*if (editedVTTSet.TextType != TextSet.TextTypeEnum.Unknown)
                    views.Add(new ManagerFileView
                    {
                        Identifier = editedVTTSet.FileIdentifier,
                        ViewerType = ManagerFileView.ViewerTypeEnum.Text,
                        Icons = new[] { "fa-comments" },
                        Label = "Edited Transcript"
                    });
                else
                    views.Add(new ManagerFileView
                    {
                        Identifier = vttSet.FileIdentifier,
                        ViewerType = ManagerFileView.ViewerTypeEnum.Text,
                        Icons = new[] { "fa-comments" },
                        Label = "Machine Transcript"
                    });
                */

                var transcriptSet = TranscriptService.TranscriptSetGet(organization, fileIdentifier, alternativeViews, extension, mimeType);
                if (transcriptSet.MediaType != MediaSet.MediaTypeEnum.Unknown)
                {
                    var view = new ManagerFileView
                    {
                        Identifier = fileIdentifier,
                        ViewerType =  ManagerFileView.ViewerTypeEnum.Transcript
                    };
                    view.Icons = new[] { "fa-comments" };

                    view.Label = "Transcript Editor";

                    views.Add(view);
                }
            }

            var textSet = TextService.TextSetGet(fileIdentifier, alternativeViews, extension, mimeType, 
                filter: n => 
                    n.MimeType == "text/plain"
                    && n.Name != "EXIF"
                );
            if (textSet.TextType != TextSet.TextTypeEnum.Unknown)
            {
                var view = new ManagerFileView
                {
                    Identifier = fileIdentifier,
                    ViewerType = ManagerFileView.ViewerTypeEnum.Text,
                    Icons = new[] { "file" }
                };

                view.Label = "Text";

                views.Add(view);
            }

            return views;
        }

    }
}
