﻿using System;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models
{
    public class JsonDeserializer
    {
        private readonly ILogger _logger;

        public JsonDeserializer(ILogger logger)
        {
            _logger = logger;
        }

        public T Deserialize<T>(string input)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(input);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Cannot deserialize object. Expected object: {ExpectedType}. Response: {Input}. Original message: {ErrorMessage}", typeof(T), input, e.Message);
                return default;
            }
        }
    }
}
