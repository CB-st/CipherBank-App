// <copyright file="HealthCheckClient.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Net.Http;
using System.Text;

namespace CipherBank_app.Services;

/// <summary>
/// Connectivity probe using the public <c>POST /test</c> endpoint (with certificate pinning).
/// </summary>
public sealed class HealthCheckClient(IHttpClientFactory httpClientFactory, TimeProvider timeProvider) : IHealthCheckClient
{
    public async Task<bool> CheckHealthAsync(string baseUrl, CancellationToken cancellationToken = default)
    {
        HttpClient client = httpClientFactory.CreateClient("HealthCheck");
        client.Timeout = TimeSpan.FromSeconds(10);

        Uri baseUri = new(baseUrl.TrimEnd('/') + "/");
        Uri testUri = new(baseUri, "test");

        using HttpRequestMessage request = new(HttpMethod.Post, testUri);
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.Date = timeProvider.GetUtcNow();
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
