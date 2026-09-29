// <copyright file="PersistenceOptions.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Persist;

namespace CipherBank_app.Configuration;

/// <summary>Settings for the on-device EF Core database.</summary>
public sealed class PersistenceOptions
{
    public string DatabaseName { get; set; } = "cipherbank.db";

    /// <summary>
    /// Demo payees inserted when the recipients table is empty. Stable ids; changing them duplicates rows.
    /// </summary>
    public IList<DefaultRecipientOptions> DefaultRecipients { get; } = new List<DefaultRecipientOptions>();

    /// <summary>
    /// Validates the database filename and every configured bootstrap recipient.
    /// Use: High (startup options validation). Scope: persistence composition.
    /// </summary>
    public bool IsValid()
        => IsDatabaseNameValid() && AreDefaultRecipientsValid();

    /// <summary>
    /// True when every seed row has a unique non-blank id and name. An empty list is valid (no seed).
    /// Use: Medium (options bind / repository construction). Scope: PersistenceOptions.
    /// </summary>
    public bool AreDefaultRecipientsValid()
    {
        List<string> ids = new(DefaultRecipients.Count);
        List<string> names = new(DefaultRecipients.Count);
        foreach (DefaultRecipientOptions row in DefaultRecipients)
        {
            if (!IsRecipientValid(row))
            {
                return false;
            }

            if (ids.Exists(id => string.Equals(id, row.Id, StringComparison.Ordinal))
                || names.Exists(name => string.Equals(name, row.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            ids.Add(row.Id);
            names.Add(row.Name);
        }

        return true;
    }

    private static bool IsRecipientValid(DefaultRecipientOptions row)
        => !string.IsNullOrWhiteSpace(row.Id)
            && AchRecipientValidation.Validate(
                row.Name,
                row.Holder ?? string.Empty,
                row.Bank ?? string.Empty,
                row.Routing ?? string.Empty,
                row.Account ?? string.Empty,
                row.AccountType,
                row.Memo) is null;

    private bool IsDatabaseNameValid()
        => !string.IsNullOrWhiteSpace(DatabaseName)
            && !Path.IsPathRooted(DatabaseName)
            && string.Equals(Path.GetFileName(DatabaseName), DatabaseName, StringComparison.Ordinal);
}
