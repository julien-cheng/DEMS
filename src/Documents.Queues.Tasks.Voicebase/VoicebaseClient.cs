//http://voicebase.readthedocs.io/en/v3/ 

namespace Documents.Queues.Tasks.Voicebase
{
    using Documents.Queues.Tasks.VoiceBase.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;

    public class VoiceBaseClient : IDisposable
    {
        private HttpClient client;

        public VoiceBaseClient(Uri uri, string token)
        {
            client = new HttpClient
            {
                BaseAddress = uri
            };
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
        }

        public async Task<string> UploadMedia(Stream stream, string callback = null)
        {
            var form = new MultipartFormDataContent();
            var fileContent = new StreamContent(stream);
            fileContent.Headers.Add("Content-Type", "audio/mp3");
            form.Add(fileContent, "media", "filename.mp3");

            if (callback != null)
                form.Add(new StringContent(JsonConvert.SerializeObject(new
                {
                    publish = new
                    {
                        callbacks = new[]
                        {
                            new
                            {
                                url = callback
                            }
                        }
                    }
                })), "configuration");

            using (var response = await client.PostAsync("media", form))
            {
                try
                {
                    response.EnsureSuccessStatusCode();
                    var fullResponse = await response.Content.ReadAsStringAsync();
                    var media = JsonConvert.DeserializeObject<Media>(fullResponse);

                    return media.MediaID;
                }
                catch (HttpRequestException)
                {
                    Console.WriteLine("Failed response: " + await response.Content.ReadAsStringAsync());
                    throw;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }
        
        public async Task DeleteMediaAsync(string mediaID)
        {
            using (var response = await client.DeleteAsync($"media/{mediaID}"))
                response.EnsureSuccessStatusCode();
        }

        public async Task<Dictionary<string, string>> DownloadTranscriptsAsync(string mediaID)
        {
            return new Dictionary<string, string>
            {
                {"application/json",  await DownloadTranscriptAsync(mediaID, null)},
                {"text/srt",  await DownloadTranscriptAsync(mediaID, "text/srt")},
                {"text/plain",  await DownloadTranscriptAsync(mediaID, "text/plain")}
            };
        }

        private async Task<string> DownloadTranscriptAsync(string mediaID, string mimeType)
        {

            Console.WriteLine($"Reading {mediaID} {mimeType}");
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri(client.BaseAddress, $"media/{mediaID}/transcripts/latest"));
            if (mimeType != null)
                request.Headers.Accept.Add(
                    new MediaTypeWithQualityHeaderValue(mimeType)
                );

            using (var response = await client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                var stringOutput = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Reading {mediaID} finished");

                return stringOutput;
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        ((IDisposable)client).Dispose();
                    }
                    catch (Exception) { }
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
