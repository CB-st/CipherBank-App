// <copyright file="GlobalSuppressions.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Diagnostics.CodeAnalysis;

// The underscore in the namespace is derived from the project name "CipherBank-app",
// which MSBuild converts to "CipherBank_app" as the root namespace.
[assembly: SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Namespace derived from project name convention", Scope = "namespace", Target = "~N:CipherBank_app.Models")]
[assembly: SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Namespace derived from project name convention", Scope = "namespace", Target = "~N:CipherBank_app.Services")]
[assembly: SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Namespace derived from project name convention", Scope = "namespace", Target = "~N:CipherBank_app.Services.Logging")]
[assembly: SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Namespace derived from project name convention", Scope = "namespace", Target = "~N:CipherBank_app.Services.Validation")]
