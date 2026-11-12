// <copyright file="NetworkEnvironmentOptions.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Configuration;

/// <summary>Per-environment API / stream endpoints.</summary>
public sealed class NetworkEnvironmentOptions
{
    public string ApiBase { get; set; } = string.Empty;

    public string PublicApiBase { get; set; } = string.Empty;

    public string StreamEndpoint { get; set; } = string.Empty;
}
