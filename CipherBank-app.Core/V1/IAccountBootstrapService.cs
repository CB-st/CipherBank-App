// <copyright file="IAccountBootstrapService.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.V1;

/// <summary>Import server bootstrap contacts/prefs for returning users. Never touches custody.</summary>
public interface IAccountBootstrapService
{
    Task ApplyAsync(CancellationToken ct);
}
