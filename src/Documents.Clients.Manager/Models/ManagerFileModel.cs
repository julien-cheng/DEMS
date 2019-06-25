namespace Documents.Clients.Manager.Models
{
    using Documents.API.Common.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System;
    using System.Collections.Generic;

    public class ManagerFileModel : ModelBase, IItemQueryResponse, IHaveAttributes
    {
        public FileIdentifier Identifier { get; set; }
        public PathIdentifier PathIdentifier { get; set; }

        public string Name { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ManagerFileView.ViewerTypeEnum ViewerType { get; set; }
        
        public long Length { get; set; }
        public string LengthForHumans { get; set; }
        public IEnumerable<string> Icons { get; set; }

        public DateTime? Created { get; set; }
        public DateTime? Modified { get; set; }

        public IEnumerable<ManagerFileView> Views { get; set; }

        public FileIdentifier PreviewImageIdentifier { get; set; }

        public IEnumerable<AllowedOperation> AllowedOperations { get; set; }

        public Dictionary<string, object> DataModel { get; set; }

        public Dictionary<string, object> Attributes { get; set; }
    }
}