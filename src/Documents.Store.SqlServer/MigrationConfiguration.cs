namespace Documents.Store.SqlServer
{
    using Documents.Common;

    public class MigrationConfiguration : IDocumentsConfiguration
    {
        public string ConnectionString { get; set; }

        public string SectionName => "DocumentsAPIConfiguration";
    }
}
