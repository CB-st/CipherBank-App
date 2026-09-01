// <copyright file="HealthCheckClient.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
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

    private readonly TimeProvider _timeProvider;

    public HealthCheckClient(IHttpClientFactory httpClientFactory, TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<bool> CheckHealthAsync(string baseUrl, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient("HealthCheck");
        client.Timeout = TimeSpan.FromSeconds(10);

        Uri baseUri = new Uri(baseUrl.TrimEnd('/') + "/");
        Uri testUri = new Uri(baseUri, "test");

        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, testUri);
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.Date = _timeProvider.GetUtcNow();
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
