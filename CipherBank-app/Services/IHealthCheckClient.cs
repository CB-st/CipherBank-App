// <copyright file="IHealthCheckClient.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Services;

/// <summary>
/// Client for probing public API connectivity with certificate pinning and app configuration.
/// </summary>
public interface IHealthCheckClient
{
    /// <summary>
    /// Checks connectivity via <c>POST /test</c> at the given base URL.
    /// Uses the app's certificate pinning and handler chain.
    /// </summary>
    Task<bool> CheckHealthAsync(string baseUrl, CancellationToken cancellationToken = default);
}
