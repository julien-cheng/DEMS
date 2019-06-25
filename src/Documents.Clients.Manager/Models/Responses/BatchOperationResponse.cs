namespace Documents.Clients.Manager.Models.Responses
{
    public class BatchOperationResponse : ModelBase
    {
        public enum Actions
        {
            None,
            ReloadPath,
            ReloadFolder,
            NavigateToUrl
        }

        public long AffectedCount { get; set; } = 0;
        public Actions SuggestedAction { get; set; }
        public string Message { get; set; } = null;
        public string Url { get; set; }
    }
}
