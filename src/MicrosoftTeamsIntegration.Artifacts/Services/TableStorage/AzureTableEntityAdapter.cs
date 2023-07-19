using System;
using System.Collections.Generic;
using System.Globalization;
using JetBrains.Annotations;
using Microsoft.Azure.Cosmos.Table;
using MicrosoftTeamsIntegration.Artifacts.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MicrosoftTeamsIntegration.Artifacts.Services.TableStorage
{
    [PublicAPI]
    public sealed class AzureTableEntityAdapter<T> : TableEntity
        where T : class
    {
        private readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            Converters = new List<JsonConverter>
            {
                new StringEnumConverter()
            }
        };

        public AzureTableEntityAdapter()
        {
            OriginalEntity = default;
        }

        [IgnoreProperty]
        public T? OriginalEntity { get; set; }

        public AzureTableEntityAdapter(T originalEntity, string partitionKey, string rowKey = "")
        {
            OriginalEntity = originalEntity ?? throw new ArgumentNullException(nameof(originalEntity));
            if (!rowKey.HasValue())
            {
                rowKey = DateTime.UtcNow.Ticks.ToString("D19", DateTimeFormatInfo.InvariantInfo);
            }

            PartitionKey = partitionKey.SanitizeForAzureKeys();
            RowKey = rowKey.SanitizeForAzureKeys();
        }

        public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            var properties = base.WriteEntity(operationContext);
            var serializedEntity = JsonConvert.SerializeObject(OriginalEntity, _jsonSerializerSettings);
            properties[nameof(OriginalEntity)] = EntityProperty.GeneratePropertyForString(serializedEntity);
            return properties;
        }

        public override void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            base.ReadEntity(properties, operationContext);

            if (properties.TryGetValue(nameof(OriginalEntity), out var property))
            {
                if (property != null)
                {
                    OriginalEntity = JsonConvert.DeserializeObject<T>(property.StringValue, _jsonSerializerSettings);
                }
            }
        }
    }
}
