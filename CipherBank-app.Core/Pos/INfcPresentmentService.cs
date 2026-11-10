// <copyright file="INfcPresentmentService.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Pos;

/// <summary>Platform NFC presentment (Android NDEF; others stub).</summary>
public interface INfcPresentmentService
{
    /// <summary>How long reader mode stays armed when a caller does not pick a window.</summary>
    public static readonly TimeSpan DefaultReaderWindow = TimeSpan.FromSeconds(30);

    bool IsSupported { get; }

    string? LastError { get; }

    Task<bool> PresentAsync(NfcPresentmentPayload payload, TimeSpan timeout, CancellationToken ct);

    /// <summary>Presents with the default reader-mode window.</summary>
    /// <param name="payload">Token reference payload to write.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True when a tag accepted the NDEF message.</returns>
    /// <remarks>Use: Medium (POS present tap). Scope: any INfcPresentmentService caller.</remarks>
    Task<bool> PresentAsync(NfcPresentmentPayload payload, CancellationToken ct)
        => PresentAsync(payload, DefaultReaderWindow, ct);

    /// <summary>Presents with an explicit window and no cancellation.</summary>
    /// <param name="payload">Token reference payload to write.</param>
    /// <param name="timeout">Reader-mode window before giving up.</param>
    /// <returns>True when a tag accepted the NDEF message.</returns>
    /// <remarks>Use: Medium (POS present tap). Scope: any INfcPresentmentService caller.</remarks>
    Task<bool> PresentAsync(NfcPresentmentPayload payload, TimeSpan timeout)
        => PresentAsync(payload, timeout, CancellationToken.None);

    /// <summary>Presents with the default window and no cancellation.</summary>
    /// <param name="payload">Token reference payload to write.</param>
    /// <returns>True when a tag accepted the NDEF message.</returns>
    /// <remarks>Use: Medium (POS present tap). Scope: any INfcPresentmentService caller.</remarks>
    Task<bool> PresentAsync(NfcPresentmentPayload payload)
        => PresentAsync(payload, DefaultReaderWindow, CancellationToken.None);
}
