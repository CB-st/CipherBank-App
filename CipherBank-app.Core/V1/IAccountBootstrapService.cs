// <copyright file="IAccountBootstrapService.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.V1;

/// <summary>Import server bootstrap contacts/prefs for returning users. Never touches custody.</summary>
public interface IAccountBootstrapService
{
    Task ApplyAsync(CancellationToken ct);
}
