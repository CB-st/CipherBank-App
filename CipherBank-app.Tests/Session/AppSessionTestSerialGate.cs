// <copyright file="AppSessionTestSerialGate.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using Xunit;

namespace CipherBank_app.Tests.Session;

/// <summary>
/// Serializes AppSession facts — unlock/bootstrap races under default xUnit parallelization
/// intermittently fail custody unlock when other suites hammer shared BCL crypto/culture paths.
/// Use: High (AppSessionTests collection). Scope: test assembly parallelization gate.
/// </summary>
[CollectionDefinition(nameof(AppSessionTests), DisableParallelization = true)]
public sealed class AppSessionTestSerialGate;
