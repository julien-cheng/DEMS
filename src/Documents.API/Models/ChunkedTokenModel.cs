namespace Documents.API.Models
{
    using Documents.API.Common.Models;
    using Newtonsoft.Json;

    public class UploadTokenModel
    {
        public UploadIdentifier Identifier { get; set; }

        public static UploadTokenModel Parse(string s)
        {
            if (s != null)
                return JsonConvert.DeserializeObject<UploadTokenModel>(s);
            else
                throw new System.Exception("Missing ChunkedToken");
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
