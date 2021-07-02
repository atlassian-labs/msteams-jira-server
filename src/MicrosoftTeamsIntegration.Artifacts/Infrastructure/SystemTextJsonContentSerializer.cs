using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Refit;

namespace MicrosoftTeamsIntegration.Artifacts.Infrastructure
{
    [PublicAPI]
    public sealed class SystemTextJsonContentSerializer : IContentSerializer
    {
        private Lazy<JsonSerializerOptions> _jsonSerializerOptions;

        public SystemTextJsonContentSerializer()
            : this(new JsonSerializerOptions())
        {
        }

        public SystemTextJsonContentSerializer(JsonSerializerOptions jsonSerializerOptions)
        {
            _jsonSerializerOptions = new Lazy<JsonSerializerOptions>(() => jsonSerializerOptions ?? new JsonSerializerOptions());
        }

        public async Task<T> DeserializeAsync<T>(HttpContent content)
        {
            using var utf8Json = await content.ReadAsStreamAsync().ConfigureAwait(false);
            return await JsonSerializer.DeserializeAsync<T>(utf8Json, _jsonSerializerOptions.Value);
        }

        public Task<HttpContent> SerializeAsync<T>(T item)
        {
            var content = new StringContent(
                JsonSerializer.Serialize(item, _jsonSerializerOptions.Value),
                Encoding.UTF8,
                "application/json");
            return Task.FromResult((HttpContent)content);
        }
    }
} 
