// <copyright file="PersistenceOptions.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Configuration;

/// <summary>Settings for the on-device EF Core database.</summary>
public sealed class PersistenceOptions
{
    public static string SectionName { get; } = "Persistence";

    public string DatabaseName { get; set; } = "cipherbank.db";

    /// <summary>
    /// Demo payees inserted when the recipients table is empty. Stable ids; changing them duplicates rows.
    /// </summary>
    public IList<DefaultRecipientOptions> DefaultRecipients { get; set; } = new List<DefaultRecipientOptions>();

    /// <summary>
    /// True when every seed row has a unique non-blank id and name. An empty list is valid (no seed).
    /// Use: Medium (options bind / repository construction). Scope: PersistenceOptions.
    /// </summary>
    public bool AreDefaultRecipientsValid()
    {
        List<string> ids = new List<string>(DefaultRecipients.Count);
        foreach (DefaultRecipientOptions row in DefaultRecipients)
        {
            if (string.IsNullOrWhiteSpace(row.Id) || string.IsNullOrWhiteSpace(row.Name))
            {
                return false;
            }

            if (ids.Exists(id => string.Equals(id, row.Id, StringComparison.Ordinal)))
            {
                return false;
            }

            ids.Add(row.Id);
        }

        return true;
    }
}
