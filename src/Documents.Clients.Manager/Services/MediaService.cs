namespace Documents.Clients.Manager.Services
{
    using Documents.API.Common.EventHooks;
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;
    using Documents.Clients.Manager.Common;
    using Documents.Clients.Manager.Models;
    using Documents.Clients.Manager.Models.Requests;
    using Documents.Clients.Manager.Models.ViewSets;
    using Documents.Queues.Messages;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class MediaService
    {
        protected readonly APIConnection Connection;

        private const string MIME_PNG = "image/png";
        private const string MIME_MP4 = "video/mp4";
        private const string MIME_MP3 = "audio/mp3";
        private const string MIME_WEBM = "video/webm";
        private const string MIME_VTT = "text/vtt";
        private const string MIME_TXT = "text/plain";

        public MediaService(APIConnection connection)
        {
            Connection = connection;
        }

        public async Task<MediaSet> MediaSetGetAsync(FileIdentifier fileIdentifier)
        {
            var file = await Connection.File.GetAsync(fileIdentifier);
            var organiation = await Connection.Organization.GetAsync(fileIdentifier as OrganizationIdentifier);

            return MediaSetGet(organiation, file);
        }

        public static MediaSet MediaSetGet(OrganizationModel organization, FileModel file, bool isPrivileged = false)
        {
            var mediaSet = MediaSetGet(organization, file.Identifier, file.Read<List<AlternativeView>>(MetadataKeyConstants.ALTERNATIVE_VIEWS), file.Extension, file.MimeType, isPrivileged: isPrivileged);

            if (mediaSet.MediaType == MediaSet.MediaTypeEnum.Audio)
            {
                var actions = FileHandling.GetFileActions(file);

                if (actions.Transcode)
                    mediaSet.Message = new UnknownSet.MessageDetails
                    {
                        Title = "Video still processing",
                        Body = "This file appears to be a video file, but so far only the " +
                            "audio is available. Please check back later if you were " +
                            "expecting video."
                    };
            }

            return mediaSet;
        }

        public static MediaSet MediaSetGet(OrganizationModel organization, FileIdentifier fileIdentifier, List<AlternativeView> alternativeViews, string extension, string mimeType, bool isPrivileged = false)
        {
            bool noTranscodes = false;

            var mediaSet = new MediaSet
            {
                AutoPlay = false,
                Preload = true,
                Poster = alternativeViews?.FirstOrDefault(v => v.MimeType == MIME_PNG)?.FileIdentifier
            };

            // query for video formats by mime-type
            var mp4 = alternativeViews?.FirstOrDefault(v => v.MimeType == MIME_MP4)?.FileIdentifier;
            var webm = alternativeViews?.FirstOrDefault(v => v.MimeType == MIME_WEBM)?.FileIdentifier;
            var mp3 = alternativeViews?.FirstOrDefault(v => v.MimeType == MIME_MP3)?.FileIdentifier;
            var exif = alternativeViews?.FirstOrDefault(v => v.Name == "EXIF")?.FileIdentifier;

            // check to see if the original was either of the targets
            if (mp4 == null && mimeType == MIME_MP4)
                mp4 = fileIdentifier;
            if (webm == null && mimeType == MIME_WEBM)
                webm = fileIdentifier;
            if (mp3 == null && mimeType == MIME_MP3)
                mp3 = fileIdentifier;

            var sources = new List<MediaSource>();
            mediaSet.Sources = sources;

            if (mp4 != null)
                sources.Add(new MediaSource(mp4, MIME_MP4));

            if (webm != null)
                sources.Add(new MediaSource(webm, MIME_WEBM));

            if (mp3 != null)
                sources.Add(new MediaSource(mp3, MIME_MP3));

            // if we don't have any of our target formats, try adding the original file
            if (!sources.Any())
            {
                sources.Add(new MediaSource(fileIdentifier, mimeType));
                noTranscodes = true;
            }

            var vtt = alternativeViews?.FirstOrDefault(v => v.MimeType == MIME_VTT && v.Name == TranscriptService.FILENAME_VTT)?.FileIdentifier;
            if (vtt == null)
                vtt = alternativeViews?.FirstOrDefault(v => v.MimeType == MIME_VTT)?.FileIdentifier;
            if (vtt != null)
            {
                mediaSet.Subtitles = new List<MediaSubtitles>
                {
                    new MediaSubtitles {
                        FileIdentifier = vtt,
                        IsDefault = true,
                        Label = "English",
                        Language = "English"
                    }
                };
            }

            // let's see how we did
            mediaSet.MediaType = MediaSet.MediaTypeEnum.Unknown;

            if (mp3 != null)
                mediaSet.MediaType = MediaSet.MediaTypeEnum.Audio;

            if (mp4 != null || webm != null)
                mediaSet.MediaType = MediaSet.MediaTypeEnum.Video;

            // we couldn't find any alternative views, indicating a transcoder
            // has visited this file. let's check the primary file itself
            if (noTranscodes && false)
            {
                if (mimeType?.ToLower().StartsWith("video") ?? false)
                    mediaSet.MediaType = MediaSet.MediaTypeEnum.Video;
                else if (mimeType?.ToLower().StartsWith("audio") ?? false)
                    mediaSet.MediaType = MediaSet.MediaTypeEnum.Audio;
            }

            var allowedOperations = new List<AllowedOperation>
            {
                AllowedOperation.GetAllowedOperationDownload(fileIdentifier, label: "Download")
            };

            if (mp4 != null)
                allowedOperations.Add(AllowedOperation.GetAllowedOperationDownload(mp4, label: "Download MP4"));

            if (exif != null)
                allowedOperations.Add(AllowedOperation.GetAllowedOperationDownload(exif, label: "Download EXIF"));

            if (mediaSet.MediaType == MediaSet.MediaTypeEnum.Audio
                && mp3 != null)
                allowedOperations.Add(AllowedOperation.GetAllowedOperationDownload(mp3, label: "Download MP3"));

            if (vtt != null)
                allowedOperations.Add(AllowedOperation.GetAllowedOperationDownload(vtt, label: "Download Subtitles"));
            else
            {
                if (isPrivileged)
                {
                    if (mp3 != null && organization.Read("transcript[isActive]", defaultValue: false))
                        allowedOperations.Add(AllowedOperation.GetAllowedOperationTranscribe(fileIdentifier));
                }
            }

            if (isPrivileged)
            {

                if (mp4 != null)
                    allowedOperations.Add(AllowedOperation.GetAllowedOperationWatermarkVideo(mp4));
            }


            mediaSet.AllowedOperations = allowedOperations;

            return mediaSet;
        }

        public async Task WatermarkVideoAsync(WatermarkVideoRequest request)
        {
            var file = await Connection.File.GetAsync(request.FileIdentifier);

            var watermarkIdentifier = new FileIdentifier(
                file.Identifier.OrganizationKey,
                ":watermarks",
                "video.png"
            );


            var parentIdentifier = file.Read<FileIdentifier>("_childof");
            if (parentIdentifier != null)
                file = await Connection.File.GetAsync(parentIdentifier);

            var fileName = file.NameWithoutExtension();
            fileName = $"{fileName}.watermarked.mp4";

            await Connection.Queue.EnqueueAsync("VideoTools", new VideoToolsMessage
            {
                Identifier = request.FileIdentifier,
                OutputName = fileName,
                Watermark = new VideoToolsMessage.WatermarkingDetails
                {
                    Watermark = watermarkIdentifier
                }
            });
        }
    }
}
