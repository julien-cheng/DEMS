namespace Documents.API
{
    using Documents.API.Common;
    using Documents.API.Common.Models.MetadataModels;
    using Documents.API.Models;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class BackendClient : IBackendClient
    {
        private static readonly HttpClient Client;
        
        private readonly ILogger<BackendClient> Logger;
        private readonly ISecurityContext SecurityContext;
        private readonly DocumentsAPIConfiguration DocumentsAPIConfiguration;

        private const int BUFFER_SIZE = 81920; // 80k

        static BackendClient()
        {
            Client = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.None
            });
        }

        public BackendClient(
            ISecurityContext securityContext,
            DocumentsAPIConfiguration documentsConfiguration,
            ILogger<BackendClient> logger
        )
        {
            this.SecurityContext = securityContext;
            this.DocumentsAPIConfiguration = documentsConfiguration;
            this.Logger = logger;
        }

        private HttpRequestMessage Request(BackendConfiguration context, HttpMethod method, string uri, object queryString = null)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (context.DriverTypeName == null)
            {
                throw new ArgumentNullException(nameof(context.DriverTypeName));
            }
            if (DocumentsAPIConfiguration.BackendGatewayURL == null)
            {
                throw new ArgumentNullException(nameof(DocumentsAPIConfiguration.BackendGatewayURL));
            }

            if (queryString != null)
            {
                // build Name=Value pairs in string[] from object properties 
                // ex. 
                //      { a = "value", b = "value2" } becomes
                //      [ "a=value", "b=value2" ]
                var type = queryString.GetType();
                var props = type.GetProperties();
                var pairs = props.Select(p => 
                    WebUtility.UrlEncode(p.Name) + "=" +
                    WebUtility.UrlEncode(
                        p.GetValue(queryString, null)?.ToString() ?? string.Empty
                    )).ToArray();

                // build pairs into URL string
                uri += "?" + string.Join("&", pairs);
            }

            var request = new HttpRequestMessage(
                method,
                new Uri(
                    new Uri(DocumentsAPIConfiguration.BackendGatewayURL),
                    uri
                )
            );

            request.Headers.Add("X-Backend-Configuration", context.ConfigurationJSON?.Replace("\n", string.Empty).Replace("\r", string.Empty));
            request.Headers.Add("X-Backend-Driver", context.DriverTypeName);

            return request;
        }

        public async Task ReadFileAsync(
            BackendConfiguration context,
            string id,
            Stream stream,
            long from,
            long to,
            long totalLength,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            using (var request = Request(context, HttpMethod.Get, $"api/backend", new {
                id,
                totalLength
            }))
            {
                var rangeHeader = new RangeHeaderValue(from, to).ToString();
                request.Headers.Add("Range", rangeHeader);
                Logger.LogDebug($"Range Header: {rangeHeader}");

                using (var response = await Client.SendAsync(
                    request, 
                    HttpCompletionOption.ResponseHeadersRead, 
                    cancellationToken))
                {
                    Logger.LogDebug($"Response started: Content-Length: {response.Content.Headers.ContentLength}");
                    response.EnsureSuccessStatusCode();


                    using (var responseStream = await response.Content.ReadAsStreamAsync())
                        await responseStream.CopyToAsync(stream, BUFFER_SIZE, cancellationToken);

                    Logger.LogDebug($"Complete");
                }
            }
        }
        
        public async Task DeleteFileAsync(
            BackendConfiguration context, 
            string id,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            using (var request = Request(context, HttpMethod.Delete, $"api/backend", new { id }))
            using (var response = await Client.SendAsync(request, cancellationToken))
            {
                response.EnsureSuccessStatusCode();
            }
        }

        public async Task<string> StartChunkedUploadAsync(
            BackendConfiguration context, 
            string id,
            ChunkedUploadModel chunkHeader,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            using (var request = Request(context, HttpMethod.Post, $"api/backend/begin" , new { id }))
            {
                request.Content = JsonContent(chunkHeader);

                using (var response = await Client.SendAsync(request, cancellationToken))
                {
                    response.EnsureSuccessStatusCode();

                    return await response.Content.ReadAsStringAsync();
                }
            }
        }

        public async Task<string> UploadChunkAsync(
            BackendConfiguration context,
            string uploadKey,
            string id,
            string chunkKey,
            int chunkIndex,
            int totalChunks,
            string sequentialState,
            long rangeFrom,
            long rangeTo,
            long rangeTotal,
            Stream stream,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            Logger.LogDebug($"SequentialState: {sequentialState}");

            using (var request = Request(context, HttpMethod.Post, $"api/backend", new
            {
                chunkIndex,
                totalChunks,
                sequentialState,
                id,
                uploadKey,
                chunkKey
            }))
            {
                Logger.LogDebug($"PutChunkedUploadAsync: {rangeFrom}-{rangeTo}/{rangeTotal}");
                using (request.Content = new StreamContent(stream))
                {
                    request.Content.Headers.ContentType = 
                        new MediaTypeHeaderValue("application/octet-stream");
                    request.Content.Headers.ContentLength = rangeTo - rangeFrom + 1;

                    request.Content.Headers.Add("X-Content-Range", 
                        new ContentRangeHeaderValue(rangeFrom, rangeTo, rangeTotal).ToString());

                    using (var response = await Client.SendAsync(request, cancellationToken))
                    {
                        response.EnsureSuccessStatusCode();
                        return await response.Content.ReadAsStringAsync();
                    }
                    
                }
            }
        }

        private HttpContent JsonContent<T>(T obj)
        {
            var content = new StringContent(JsonConvert.SerializeObject(obj, Formatting.Indented), Encoding.UTF8, "application/json");
            return content;
        }

        private async Task<T> ReadAsJsonObjectAsync<T>(HttpResponseMessage response)
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(json);
        }

        public async Task<IDictionary<string, object>> CompleteChunkedUploadAsync(
            BackendConfiguration context,
            string uploadKey,
            string id,
            ChunkedStatusModel[] chunkStatuses,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            using (var request = Request(context, HttpMethod.Post, $"api/backend/end", new
            {
                uploadKey,
                id
            }))
            {
                
                request.Content = JsonContent(chunkStatuses);

                using (var response = await Client.SendAsync(request, cancellationToken))
                {
                    response.EnsureSuccessStatusCode();

                    return await ReadAsJsonObjectAsync<Dictionary<string, object>>(response);
                }
            }
        }

        public async Task<bool> CheckHealthAsync(BackendConfiguration context)
        {
            using (var request = Request(context, HttpMethod.Delete, $"api/healthcheck"))
            using (var response = await Client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
            }

            return true;
        }

        public async Task<bool> SetTagsAsync(BackendConfiguration context, string id, Dictionary<string, string> tags, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var request = Request(context, HttpMethod.Post, $"api/backend/tag", new { id }))
            {
                request.Content = JsonContent(tags);
                using (var response = await Client.SendAsync(request, cancellationToken))
                    response.EnsureSuccessStatusCode();
            }

            return true;
        }

        public async Task<Dictionary<string, string>> GetTagsAsync(BackendConfiguration context, string id, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var request = Request(context, HttpMethod.Get, $"api/backend/tag", new { id }))
            {
                using (var response = await Client.SendAsync(request, cancellationToken))
                {
                    response.EnsureSuccessStatusCode();
                    return await ReadAsJsonObjectAsync<Dictionary<string, string>>(response);
                }
            }
        }

        public async Task<FileBackendConstants.OnlineStatus> GetOnlineStatusAsync(BackendConfiguration context, string id, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var request = Request(context, HttpMethod.Get, $"api/backend/online", new { id }))
            using (var response = await Client.SendAsync(request, cancellationToken))
            {
                response.EnsureSuccessStatusCode();
                return await ReadAsJsonObjectAsync<FileBackendConstants.OnlineStatus>(response);
            }
        }

        public async Task<bool> RequestOnlineAsync(BackendConfiguration context, string id, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var request = Request(context, HttpMethod.Post, $"api/backend/online", new { id }))
            using (var response = await Client.SendAsync(request, cancellationToken))
                response.EnsureSuccessStatusCode();

            return true;
        }
    }
}
