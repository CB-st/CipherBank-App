// <copyright file="ProductAuthHeaderHandler.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Net.Http.Headers;

namespace CipherBank_app.V1;

/// <summary>
/// Injects Bearer access tokens from <see cref="IProductSessionStore"/> onto product HTTP calls.
/// </summary>
public sealed class ProductAuthHeaderHandler : DelegatingHandler
{
    private readonly ProductSessionAuthority _sessions;

    public ProductAuthHeaderHandler(
        IProductSessionStore sessions)
        : this(sessions, null)
    {
    }

    public ProductAuthHeaderHandler(IProductSessionStore sessions, ProductSessionAuthority? authority)
    {
        _sessions = authority ?? ProductSessionAuthority.For(sessions);
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
        ProductSessionSnapshot? stored = await ProductSessionDispatch.ReadAsync(request, _sessions, CancellationToken.None).ConfigureAwait(false);
        if (stored is { } session && !string.IsNullOrWhiteSpace(session.Access))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.Access);
        }

        if (stored is not null)
        {
            _sessions.Check(stored.Operation, CancellationToken.None);
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
