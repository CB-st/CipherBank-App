// <copyright file="CoraLineProvider.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Configuration;
using Microsoft.Extensions.Options;

namespace CipherBank_app.Cora;

/// <inheritdoc />
public sealed class CoraLineProvider : ICoraLineProvider
{
    private readonly CoraOptions _options;
    private readonly Dictionary<string, string> _lines;

    public CoraLineProvider(IOptions<CoraOptions> options)
    {
        _options = options.Value;
        _lines = new Dictionary<string, string>(_options.Lines, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public string GetLine(string screen)
        => _lines.TryGetValue(screen, out string? line) ? line : _options.Fallback;
}
