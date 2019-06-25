namespace Documents.Backends.Drivers.FileSystem
{
    using Newtonsoft.Json;

    public class Context
    {
        public string BasePath { get; set; }
        internal void Configure(string json)
        {
            JsonConvert.PopulateObject(json, this);
        }
    }
}
