namespace Documents.Clients.Admin.Models
{
    using System;
    using System.Collections.Generic;

    public class ReportModel
    {
        public string OrganizationKey{ get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public List<ReportRow> Report { get; set; }
    }
}
