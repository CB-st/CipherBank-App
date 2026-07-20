// <copyright file="HealthCheckClient.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Net.Http;
using System.Text;

namespace CipherBank_app.Services;

/// <summary>
/// Connectivity probe using the public <c>POST /test</c> endpoint (with certificate pinning).
/// </summary>
public sealed class HealthCheckClient : IHealthCheckClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public HealthCheckClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<bool> CheckHealthAsync(string baseUrl, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient("HealthCheck");
        client.Timeout = TimeSpan.FromSeconds(10);

        var baseUri = new Uri(baseUrl.TrimEnd('/') + "/");
        var testUri = new Uri(baseUri, "test");

        using var request = new HttpRequestMessage(HttpMethod.Post, testUri);
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.Date = DateTimeOffset.UtcNow;
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
