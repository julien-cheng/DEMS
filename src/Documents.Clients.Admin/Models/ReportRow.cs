namespace Documents.Clients.Admin.Models
{
    using System;

    public class ReportRow
    {
        public DateTime Date { get; set; }
        public string User { get; set; }
        public string OrganizationKey { get; set; }
        public string OrganizationName { get; set; }
        public int Duration { get; set; }
        public string Status { get; set; }
        public string MediaID { get; set; }
        public string Folder { get; set; }
        public string File { get; set; }
        public string Name { get; set; }
    }
}
