// <copyright file="HealthCheckClient.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Net.Http;

namespace CipherBank_app.Services;

/// <summary>
/// Health check client using the app's configured HTTP handler (certificate pinning).
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
        var healthUri = new Uri(baseUri, "health");
        var response = await client.GetAsync(healthUri, cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
