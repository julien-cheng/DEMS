namespace Documents.Queues.Messages
{
    using Documents.API.Common.Models;

    public class SynchronizeMessage
    {
        public OrganizationIdentifier OrganizationIdentifier { get; set; }
        public string Component { get; set; }
        public int Key { get; set; }
        public string Result { get; set; }
        public string ConfigurationHash { get; set; }
    }
}
