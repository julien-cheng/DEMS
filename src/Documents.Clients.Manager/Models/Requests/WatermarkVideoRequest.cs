namespace Documents.Clients.Manager.Models.Requests
{
    using Documents.API.Common.Models;

    public class WatermarkVideoRequest : ModelBase
    {
        public FileIdentifier FileIdentifier { get; set; }
    }
}