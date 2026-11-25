// <copyright file="IAccountBootstrapStateStore.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>Atomically persists account-bootstrap preferences and recipient rows.</summary>
public interface IAccountBootstrapStateStore
{
    /// <summary>Commits the merged preferences and eligible recipients as one local unit.</summary>
    Task CommitAsync(UserPrefs prefs, IReadOnlyList<AchRecipientRow> recipients, CancellationToken ct);
}
