// <copyright file="ProductAuthHeaderHandler.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Net.Http.Headers;

namespace CipherBank_app.V1;

/// <summary>
/// Injects Bearer access tokens from <see cref="IProductSessionStore"/> onto product HTTP calls.
/// </summary>
public sealed class ProductAuthHeaderHandler : DelegatingHandler
{
    private readonly IProductSessionStore _sessions;

    public ProductAuthHeaderHandler(IProductSessionStore sessions)
    {
        _sessions = sessions;
    }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return SendWithAuthAsync(request, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendWithAuthAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        (string Access, string Refresh, DateTimeOffset Expires)? stored =
            await _sessions.GetAsync().ConfigureAwait(false);
        if (stored is { } session && !string.IsNullOrWhiteSpace(session.Access))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.Access);
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
