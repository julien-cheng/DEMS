namespace Documents.Clients.Manager.Models.Requests
{
    using System.ComponentModel;

    public class ItemQueryRequest : ModelBase
    {
        public PathIdentifier PathIdentifier { get; set; }

        [DefaultValue(0)]
        public int PageIndex { get; set; } = 0;

        [DefaultValue(500)]
        public int PageSize { get; set; } = 500;

        [DefaultValue(nameof(IItemQueryResponse.Name))]
        public string SortField { get; set; } = nameof(IItemQueryResponse.Name);

        [DefaultValue(true)]
        public bool SortAscending { get; set; } = true;
    }
}
