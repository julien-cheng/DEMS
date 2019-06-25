namespace Documents.Queues.Tasks.Voicebase
{
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;
    using Documents.Queues.Tasks.VoiceBase.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    class VoicebaseCallbackTask :
        QueuedApplication<VoicebaseTask.VoicebaseConfiguration, CallbackModel>
    {
        protected override string ConfigurationSectionName => "DocumentsQueuesTasksVoicebase";
        protected override string QueueName => "Voicebase.Callback";

        protected override async Task Process()
        {
            // get the callback response file
            var file = await API.File.GetAsync(CurrentMessage.FileIdentifier);
            var originalFileIdentifier = file.Read<FileIdentifier>(MetadataKeyConstants.CHILDOF);
            var originalFile = await API.File.GetAsync(originalFileIdentifier);

            var maxDepth = 10;
            var parent = originalFile.Read<FileIdentifier>(MetadataKeyConstants.CHILDOF);
            while (parent != null && maxDepth-- > 0)
            {
                originalFileIdentifier = parent;
                originalFile = await API.File.GetAsync(parent);
                parent = originalFile.Read<FileIdentifier>(MetadataKeyConstants.CHILDOF);
            }

            // download the callback response file and deserialize it
            var obj = await API.File.DownloadAsAsync<MediaResponse>(CurrentMessage.FileIdentifier);

            var message = $"Transcription of file {originalFile.Name} completed";

            await API.Log.PostAsync(new AuditLogEntryModel
            {
                Identifier = new AuditLogEntryIdentifier(originalFileIdentifier),
                FileIdentifier = originalFileIdentifier,
                ActionType = "Transcription",
                Description = message,
                Details = JsonConvert.SerializeObject(new
                {
                    obj.MediaID,
                    obj.Status,
                    obj.Length
                }),
                InitiatorUserIdentifier = originalFile.Read<UserIdentifier>("attribute.requestedBy") ?? API.UserIdentifier,
                Generated = DateTime.UtcNow,
                UserAgent = API.UserAgent
            });

            // grab the VTT formatted transcript
            var vttContents = Convert.FromBase64String(obj.Transcript.AlternateFormats.First(a => a.Format == "webvtt").Data);
            // upload the vtt transcript
            var vttFile = new FileModel
            {
                Identifier = new FileIdentifier(
                    originalFileIdentifier as FolderIdentifier,
                    Guid.NewGuid().ToString()
                ),
                Name = Path.GetFileNameWithoutExtension(originalFile.Name) + ".vtt",
                MimeType = "text/vtt"
            };

            vttFile
                .InitializeEmptyMetadata()
                .Write(MetadataKeyConstants.CHILDOF, originalFileIdentifier)
                .Write(MetadataKeyConstants.HIDDEN, true);

            vttFile = await API.File.UploadAsync(vttFile, Encoding.UTF8.GetString(vttContents));

            await API.ConcurrencyRetryBlock(async () =>
            {
                // tag the original
                originalFile = await API.File.GetAsync(originalFileIdentifier);

                var views = originalFile.Read(MetadataKeyConstants.ALTERNATIVE_VIEWS, defaultValue: new List<AlternativeView>());
                views.Add(new AlternativeView
                {
                    FileIdentifier = vttFile.Identifier,
                    MimeType = "text/vtt",
                    Name = "Voicebase WebVTT",
                });
                originalFile.Write(MetadataKeyConstants.ALTERNATIVE_VIEWS, views);
                originalFile.Write("attributes.voicebase.status", "complete");

                await API.File.PutAsync(originalFile);
            });

            using (var voicebase = new VoiceBaseClient(new Uri(Configuration.VoicebaseURL), Configuration.VoicebaseToken))
                await voicebase.DeleteMediaAsync(obj.MediaID);
        }
    }
}