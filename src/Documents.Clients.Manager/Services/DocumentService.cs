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

    public class DocumentSetService
    {
        protected readonly APIConnection Connection;

        private const string MIME_PDF = "application/pdf";
        private const string MIME_PDF_BAD_MIGRATION = "PDF";

        public DocumentSetService(APIConnection connection)
        {
            Connection = connection;
        }

        public async Task<DocumentSet> DocumentSetGetAsync(FileIdentifier fileIdentifier)
        {
            var file = await Connection.File.GetAsync(fileIdentifier);
            return DocumentSetGet(file);
        }

        public static DocumentSet DocumentSetGet(FileModel file)
        {
            return DocumentSetGet(file.Identifier, file.Read<List<AlternativeView>>(MetadataKeyConstants.ALTERNATIVE_VIEWS), file.Extension, file.MimeType);
        }

        public static DocumentSet DocumentSetGet(FileIdentifier fileIdentifier, List<AlternativeView> alternativeViews, string extension, string mimeType)
        {
            var allowedOperations = new List<AllowedOperation>
            {
                AllowedOperation.GetAllowedOperationDownload(fileIdentifier, label: "Download"),
                AllowedOperation.GetAllowedOperationSave(fileIdentifier)
            };

            var documentSet = new DocumentSet
            {
                DocumentType = DocumentSet.DocumentTypeEnum.Unknown
            };

            // query for document formats by mime-type
            var pdf = alternativeViews?.FirstOrDefault(v => v.MimeType == MIME_PDF && v.Name == "Searchable PDF")?.FileIdentifier;
            if (pdf != null)
                allowedOperations.Add(AllowedOperation.GetAllowedOperationDownload(pdf, label: "Download Searchable"));
            else
                pdf = alternativeViews?.FirstOrDefault(v => v.MimeType == MIME_PDF)?.FileIdentifier;

            if (pdf == null)
                pdf = alternativeViews?.FirstOrDefault(v => v.MimeType == MIME_PDF_BAD_MIGRATION)?.FileIdentifier;

            if (pdf != null)
                allowedOperations.Add(AllowedOperation.GetAllowedOperationDownload(pdf, label: "Download PDF"));

            // check to see if the original was pdf
            if (pdf == null && (mimeType == MIME_PDF || extension == "pdf"))
                pdf = fileIdentifier;

            if (pdf != null)
            {
                documentSet.DocumentType = DocumentSet.DocumentTypeEnum.PDF;
                documentSet.FileIdentifier = pdf;
            }

            documentSet.AllowedOperations = allowedOperations;

            return documentSet;
        }
    }
}
