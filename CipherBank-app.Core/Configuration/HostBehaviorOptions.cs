// <copyright file="HostBehaviorOptions.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Configuration;

/// <summary>Runtime host policy selected by build/environment overlays.</summary>
public sealed class HostBehaviorOptions
{
    private static readonly HashSet<string> _validLogLevels =
        ["Verbose", "Debug", "Information", "Warning", "Error", "Fatal"];

    public bool UseMockServices { get; init; }

    public bool IncludeDiagnosticHeaders { get; init; }

    public bool ShowDevelopmentIndicators { get; init; }

    public string MinimumLogLevel { get; init; } = "Information";

    public bool EnableFileLogging { get; init; } = true;

    public bool IsValid() => _validLogLevels.Contains(MinimumLogLevel);
}
