// <copyright file="HealthCheckClient.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Services;

/// <summary>
/// Health check client using the app's configured HTTP handler (certificate pinning).
/// </summary>
public sealed class HealthCheckClient(IHttpClientFactory httpClientFactory) : IHealthCheckClient
{
    public async Task<bool> CheckHealthAsync(string baseUrl, CancellationToken cancellationToken = default)
    {
        HttpClient client = httpClientFactory.CreateClient("HealthCheck");
        client.Timeout = TimeSpan.FromSeconds(10);

        Uri baseUri = new(baseUrl.TrimEnd('/') + "/");
        Uri healthUri = new(baseUri, "health");
        HttpResponseMessage response = await client.GetAsync(healthUri, cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
