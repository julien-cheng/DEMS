namespace Documents.Clients.PCMSBridge.Models
{
    using Newtonsoft.Json;
    using System;

    public class TokenState
    {
        public string Token { get; set; }
        public string SequentialState { get; set; }

        public long Position { get; set; }
        public long TotalLength { get; set; }

        public TokenState(string state = null)
        {
            if (state != null)
                JsonConvert.PopulateObject(state, this);
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
