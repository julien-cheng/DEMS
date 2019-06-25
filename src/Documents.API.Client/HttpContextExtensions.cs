namespace Documents.API.Client
{
    using Newtonsoft.Json;
    using System.Net.Http;
    using System.Threading.Tasks;

    internal static class HttpContextExtensions
    {
        public async static Task<T> ReadAsAsync<T>(this HttpContent content)
        {
            var json = await content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
