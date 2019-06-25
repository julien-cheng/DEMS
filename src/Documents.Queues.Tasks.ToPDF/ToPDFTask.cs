namespace Documents.Queues.Tasks.ToPDF
{
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;
    using Documents.Queues.Messages;
    using Documents.Queues.Tasks.ToPDF.Configuration;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Globalization;
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;

    class ToPDFTask :
        QueuedApplication<ToPDFConfiguration, ConvertToPDFMessage>
    {
        protected override string ConfigurationSectionName => "DocumentsQueuesTasksToPDF";
        protected override string QueueName => "ToPDF";
        private HttpClient Client = new HttpClient();
        private const int MAXIMUM_FILE_SIZE = 1024 * 1024 * 50;

        protected override async Task Process()
        {
            var originalFileModel = await GetFileAsync();
            await base.DownloadAsync();
            var pdfFileName = Path.GetTempFileName();

            Logger.LogInformation("uploading to unoconv");

            using (var content = new MultipartFormDataContent(
                "Upload----" + DateTime.Now.ToString(CultureInfo.InvariantCulture))
            )
            using (var fs = new FileStream(base.LocalFilePath, FileMode.Open))
            {
                content.Add(new StreamContent(fs), "file", originalFileModel.Name);

                using (var response = await Client.PostAsync(Configuration.UnoConvUri, content))
                {
                    response.EnsureSuccessStatusCode();

                    using (var pdfStream = await response.Content.ReadAsStreamAsync())
                    using (var output = new FileStream(pdfFileName, FileMode.Open, FileAccess.Write))
                        await pdfStream.CopyToAsync(output);
                }

                var newFile = await API.File.FileModelFromLocalFileAsync(
                    pdfFileName, 
                    new FileIdentifier(
                        originalFileModel.Identifier as FolderIdentifier, 
                        null
                    )
                );
                newFile.Name = Path.GetFileNameWithoutExtension(originalFileModel.Name) + ".pdf";
                newFile.MimeType = "application/pdf";
                newFile.Write(MetadataKeyConstants.HIDDEN, true);
                newFile.Write(MetadataKeyConstants.CHILDOF, originalFileModel.Identifier);

                newFile = await API.File.UploadLocalFileAsync(pdfFileName, newFile);

                var alternativeView = new AlternativeView()
                {
                    FileIdentifier = newFile.Identifier,
                    MimeType = "application/pdf",
                    Name = "pdf"
                };

                await base.TagAlternativeView(originalFileModel.Identifier, newFile.Identifier, alternativeView);
            }
        }
    }
}