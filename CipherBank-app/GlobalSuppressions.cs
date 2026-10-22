// <copyright file="GlobalSuppressions.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Diagnostics.CodeAnalysis;

// The underscore in the namespace is derived from the project name "CipherBank-app",
// which MSBuild converts to "CipherBank_app" as the root namespace.
[assembly: SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Namespace derived from project name convention", Scope = "namespace", Target = "~N:CipherBank_app")]
[assembly: SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Namespace derived from project name convention", Scope = "namespace", Target = "~N:CipherBank_app.Constants")]
[assembly: SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Namespace derived from project name convention", Scope = "namespace", Target = "~N:CipherBank_app.Extensions")]
[assembly: SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Namespace derived from project name convention", Scope = "namespace", Target = "~N:CipherBank_app.Models")]
[assembly: SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Namespace derived from project name convention", Scope = "namespace", Target = "~N:CipherBank_app.Services")]
[assembly: SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Namespace derived from project name convention", Scope = "namespace", Target = "~N:CipherBank_app.Services.Handlers")]
[assembly: SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Namespace derived from project name convention", Scope = "namespace", Target = "~N:CipherBank_app.Services.Mocks")]
[assembly: SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Namespace derived from project name convention", Scope = "namespace", Target = "~N:CipherBank_app.ViewModels")]
[assembly: SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Namespace derived from project name convention", Scope = "namespace", Target = "~N:CipherBank_app.Views")]
[assembly: SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Namespace derived from project name convention", Scope = "namespace", Target = "~N:CipherBank_app.Platforms.MacCatalyst")]

// AppDelegate is the standard name for MAUI/iOS application delegates
[assembly: SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "AppDelegate is the standard name for MAUI/iOS application delegates", Scope = "type", Target = "~T:CipherBank_app.AppDelegate")]
