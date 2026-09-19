// <copyright file="HostBehaviorOptions.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Configuration;

/// <summary>Runtime host policy selected by build/environment overlays.</summary>
public sealed class HostBehaviorOptions
{
    public bool UseMockServices { get; set; }

    public bool IncludeDiagnosticHeaders { get; set; }

    public bool ShowDevelopmentIndicators { get; set; }

    public string MinimumLogLevel { get; set; } = "Information";

    public bool EnableFileLogging { get; set; } = true;

    public bool IsValid() =>
        MinimumLogLevel is "Verbose" or "Debug" or "Information" or "Warning" or "Error" or "Fatal";
}
