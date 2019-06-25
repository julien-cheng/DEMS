namespace Documents.Clients.Manager.Models
{
    public class ManagerFieldsMetadataModel : ModelBase
    {
        public const string METADATA_KEY_LIST = "_fields";

        public string Key { get; set; }
        public string Identifier { get; set; }
        public string Name { get; set; }
    }
}