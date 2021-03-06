﻿using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Messages;
using Streetcred.Sdk.Models;
using Streetcred.Sdk.Utils;

namespace Streetcred.Sdk.Runtime
{
    /// <inheritdoc />
    public class DefaultRouterService : IRouterService
    {
        private readonly IMessageSerializer _messageSerializer;
        private readonly ILogger<DefaultRouterService> _logger;
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Streetcred.Sdk.Runtime.DefaultRouterService"/> class.
        /// </summary>
        public DefaultRouterService(IMessageSerializer messageSerializer, ILogger<DefaultRouterService> logger)
        {
            _messageSerializer = messageSerializer;
            _logger = logger;
            _httpClient = new HttpClient();
        }

        /// <inheritdoc />
        public async Task ForwardAsync(IEnvelopeMessage envelope, AgentEndpoint endpoint)
        {
            _logger.LogInformation(LoggingEvents.Forward, "Envelope {0}, Endpoint {1}", envelope.Type, endpoint.Uri);

            var encrypted = await _messageSerializer.PackAsync(envelope, endpoint.Verkey);

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(endpoint.Uri),
                Method = HttpMethod.Post,
                Content = new ByteArrayContent(encrypted)
            };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }
    }
}
