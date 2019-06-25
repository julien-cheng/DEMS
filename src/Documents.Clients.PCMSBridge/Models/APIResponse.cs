namespace Documents.Clients.PCMSBridge.Models
{
    public class PCMSAPIResponse
    {
        public class StateFields
        {
            public string UserState { get; set; }
            public string CountyState { get; set; }
        }
        public StateFields State { get; set; }
    }

    public class PCMSAPIResponse<T> : PCMSAPIResponse
    {
        public T Response { get; set; }
    }
}