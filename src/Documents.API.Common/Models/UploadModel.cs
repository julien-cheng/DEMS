namespace Documents.API.Common.Models
{
    using System.Collections.Generic;

    public class UploadModel : IHasIdentifier<UploadIdentifier>
    { 
        public UploadIdentifier Identifier { get; set;}

        public long Length { get; set; }

        public PagedResults<UploadChunkModel> Chunks { get; set; }
    }
}
