namespace Documents.API.Common.Models
{
    public class PagingArguments
    {
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public string SortField { get; set; }
        public bool IsAscending { get; set; }
    }
}