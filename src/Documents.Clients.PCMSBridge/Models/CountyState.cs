namespace Documents.Clients.PCMSBridge.Models
{
    using Documents.API.Common.Models;
    using Newtonsoft.Json;
    using System;

    public class CountyState
    {
        public static CountyState Read(string json)
        {

            CountyState state = null;
            
            if (json != null)
                state = JsonConvert.DeserializeObject<CountyState>(json);
            else
                throw new Exception("Invalid Configuration");

            if (state?.UserIdentifier?.IsValid ?? false)
                return state;
            else
                throw new Exception("Invalid Configuration");
        }

        public UserIdentifier UserIdentifier { get; set; }
        public string UserSecret { get; set; }

        public string Save()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
