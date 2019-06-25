namespace Documents.Store.SqlServer.Entities
{
    using System;

    public interface IHaveAuditDates
    {
        DateTime Created { get; set; }
        DateTime Modified { get; set; }
    }
}
