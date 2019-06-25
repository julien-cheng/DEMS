namespace Documents.Queues.Tasks.Voicebase
{
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;
    using Documents.Queues.Messages;
    using Documents.Queues.Tasks.Configuration;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    class VoicebaseTask :
        QueuedApplication<VoicebaseTask.VoicebaseConfiguration, TranscribeMessage>
    {
        protected override string ConfigurationSectionName => "DocumentsQueuesTasksVoicebase";
        protected override string QueueName => "Voicebase";

        protected override async Task Process()
        {
            var file = await GetFileAsync();
            var fileOutput = await API.File.PutAsync(new FileModel
                {
                    Identifier = new FileIdentifier(file.Identifier as FolderIdentifier, Guid.NewGuid().ToString()),
                    MimeType = "text/plain",
                    Name = "Voicebase output"
                }
                .InitializeEmptyMetadata()
                .Write(MetadataKeyConstants.HIDDEN, true)
                .Write(MetadataKeyConstants.CHILDOF, file.Identifier));

            var callbackUrl = await API.Queue.CreateCallbackAsync(new CallbackModel
            {
                FileIdentifier = fileOutput.Identifier,
                Queue = "Voicebase.Callback",
                Token = API.Token
            });

            var alternativeViews = file.Read<List<AlternativeView>>(MetadataKeyConstants.ALTERNATIVE_VIEWS);
            var mp3 = alternativeViews?.FirstOrDefault(a => a.MimeType == "audio/mp3");

            var targetFileIdentifier = mp3 == null
                ? file.Identifier
                : mp3.FileIdentifier;

            using (var voicebase = new VoiceBaseClient(new Uri(Configuration.VoicebaseURL), Configuration.VoicebaseToken))
                await API.File.DownloadAsync(targetFileIdentifier, async (stream, cancel) =>
                {
                    var mediaID = await voicebase.UploadMedia(stream, callbackUrl);

                    await API.ConcurrencyRetryBlock(async () =>
                    {
                        file = await GetFileAsync();
                        file.Write("attributes.voicebase.status", "processing");
                        file.Write("attributes.voicebase.mediaid", mediaID);
                        file.Write("attributes.requestedBy", CurrentMessage.RequestedBy);
                        await API.File.PutAsync(file);
                    });
                });
        }

        public class VoicebaseConfiguration : TaskConfiguration
        {
            public override string SectionName => "DocumentsQueuesTasksVoicebase";

            public string VoicebaseURL { get; set; }
            public string VoicebaseToken { get; set; }
        }
    }
}