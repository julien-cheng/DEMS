namespace Documents.Clients.Manager.Models.Responses
{
    using System.Collections.Generic;

    public class GridViewModel : IViewModel
    {
        public readonly static string GRID_TITLE_FILES = "Files";
        public readonly static string GRID_TITLES_EDISOVERY_PACKAGES = "eDiscovery Packages";
        public readonly static string GRID_TITLES_EDISOVERY_RECIPIENTS = "eDiscovery Recipients";
        public readonly static string GRID_TITLES_LEO_UPLOAD_OFFICERS = "LEO Upload Officers";
        public readonly static string GRID_TITLES_EDISOVERY_AUDIT_LOG = "Audit Log";

        public string Type => "Grid";

        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int PageCount { get; set; }
        public int RowsInPage { get; set; }
        public int TotalRows { get; set; }
        public bool IsLastPage { get; set; }

        public List<AllowedOperation> AllowedOperations { get; set; }

        public IEnumerable<GridColumnSpecification> GridColumns { get; set; }
        public IEnumerable<IItemQueryResponse> Rows { get; set; }
    }
}
