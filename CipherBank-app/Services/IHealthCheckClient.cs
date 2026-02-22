// <copyright file="IHealthCheckClient.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Services;

/// <summary>
/// Client for health-checking API connectivity with certificate pinning and app configuration.
/// </summary>
public interface IHealthCheckClient
{
    /// <summary>
    /// Checks connectivity to the health endpoint at the given base URL.
    /// Uses the app's certificate pinning and handler chain.
    /// </summary>
    Task<bool> CheckHealthAsync(string baseUrl, CancellationToken cancellationToken = default);
}
