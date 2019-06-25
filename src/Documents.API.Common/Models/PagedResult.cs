namespace Documents.API.Common.Models
{
    using System;
    using System.Collections.Generic;

    public class PagedResults<TModel>
    {
        public IEnumerable<TModel> Rows { get; set; }
        public long TotalMatches { get; set; }
    }
}
