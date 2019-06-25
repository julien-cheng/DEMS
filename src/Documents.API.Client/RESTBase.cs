namespace Documents.API.Client
{
    using Documents.API.Common;
    using Documents.API.Common.Models;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class RESTBase<TModel, TIdentifier>
        where TModel : class, IHasIdentifier<TIdentifier>
        where TIdentifier: IIdentifier
    {
        protected readonly Connection Connection;
        protected readonly APIEndpoint Endpoint;

        public RESTBase(Connection connection, APIEndpoint endpoint)
        {
            this.Connection = connection;
            this.Endpoint = endpoint;
        }

        public async Task<TModel> GetOrThrowAsync(
            TIdentifier identifier,
            IEnumerable<PopulationDirective> population = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return await Connection.APICallAsync<TModel>(
                HttpMethod.Get,
                this.Endpoint,
                queryStringContent: new { identifier, population },
                onResponse: ETagResponseHandler,
                cancellationToken: cancellationToken
            );
        }

        public virtual async Task<TModel> GetAsync(
            TIdentifier identifier,
            IEnumerable<PopulationDirective> population = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return await Connection.APICallAsync<TModel>(
                HttpMethod.Get,
                this.Endpoint,
                queryStringContent: new { identifier, population },
                onResponse: async (response) =>
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        return null;
                    else
                        return await ETagResponseHandler(response);
                },
                cancellationToken: cancellationToken
            );
        }

        public async Task<TModel> ETagResponseHandler(HttpResponseMessage response)
        {
            TModel model = await Connection.DefaultResponseHandler<TModel>(response);


            if (model is IProvideETag && model != null)
            {
                var etag = response.Headers?.ETag?.Tag;
                
                // it's wrapped in quotes and for some reason parsing gives up there
                if (etag != null && etag.StartsWith("\"") && etag.EndsWith("\"") && etag.Length >= 2)
                    etag = etag.Substring(1, etag.Length - 2);

                ((IProvideETag)model).ETag = etag;
            }

            return model;
        }

        public virtual async Task<TModel> PutAsync(TModel model, CancellationToken cancellationToken = default(CancellationToken))
        {
            var headers = new Dictionary<string, string>();

            var etagProvider = (model as IProvideETag);
            // if the model has an etag, we'll use this for
            // optimistic locking
            var etag = etagProvider?.ETag;
            if (etag != null)
                headers.Add("If-Match", $"\"{etag}\"");

            model = await Connection.APICallAsync<TModel>(
                HttpMethod.Put,
                this.Endpoint,
                bodyContent: model,
                headers: headers,
                onResponse: ETagResponseHandler,
                cancellationToken: cancellationToken
            );

            return model;
        }

        public virtual async Task<TModel> PostAsync(TModel model, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await Connection.APICallAsync<TModel>(
                HttpMethod.Post,
                this.Endpoint,
                bodyContent: model,
                onResponse: ETagResponseHandler,
                cancellationToken: cancellationToken
            );
        }

        public virtual async Task<TModel> DeleteAsync(TIdentifier identifier, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await Connection.APICallAsync<TModel>(
                HttpMethod.Delete,
                this.Endpoint,
                queryStringContent: new { identifier },
                cancellationToken: cancellationToken
            );
        }
    }
}
