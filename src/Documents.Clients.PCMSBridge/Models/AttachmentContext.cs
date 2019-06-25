namespace Documents.Clients.PCMSBridge.Models
{
    public class AttachmentContext
    {
        public string EmailAddress { get; set; }
        public string DMSLogin { get; set; }
        public int? DefendantID { get; set; }
        public string DefendantFirstName { get; set; }
        public string DefendantLastName { get; set; }
        public string CaseNumber { get; set; }

        public int CountyID { get; set; }
        public string County { get; set; }

        public string UserState { get; set; }
        public string CountyState { get; set; }
    }
}