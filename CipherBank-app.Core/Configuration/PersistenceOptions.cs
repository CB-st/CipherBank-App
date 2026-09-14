// <copyright file="PersistenceOptions.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Configuration;

/// <summary>Settings for the on-device EF Core database.</summary>
public sealed class PersistenceOptions : IOptionsSection
{
    private const int AccountNumberMinLength = 4;
    private const int RoutingNumberLength = 9;
    private const int MemoMaxLength = 140;

    public static string SectionName { get; } = "Persistence";

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
    {
        if (!HasRecipientIdentity(row) || !HasBankIdentity(row))
        {
            return false;
        }

        if (!IsRoutingValid(row.Routing) || !IsAccountValid(row.Account))
        {
            return false;
        }

        return IsAccountTypeValid(row.AccountType)
            && (row.Memo is null || row.Memo.Length <= MemoMaxLength);
    }

    private static bool HasRecipientIdentity(DefaultRecipientOptions row)
        => !string.IsNullOrWhiteSpace(row.Id)
            && !string.IsNullOrWhiteSpace(row.Name);

    private static bool HasBankIdentity(DefaultRecipientOptions row)
        => !string.IsNullOrWhiteSpace(row.Holder)
            && !string.IsNullOrWhiteSpace(row.Bank);

    private static bool IsAccountValid(string? account)
        => !string.IsNullOrWhiteSpace(account)
            && account.Trim().Length >= AccountNumberMinLength;

    private static bool IsAccountTypeValid(string accountType)
        => string.Equals(accountType, "checking", StringComparison.OrdinalIgnoreCase)
            || string.Equals(accountType, "savings", StringComparison.OrdinalIgnoreCase);

    private static bool IsRoutingValid(string? routing)
        => routing is not null
            && routing.Length == RoutingNumberLength
            && routing.All(static character => character is >= '0' and <= '9');

    private bool IsDatabaseNameValid()
        => !string.IsNullOrWhiteSpace(DatabaseName)
            && !Path.IsPathRooted(DatabaseName)
            && string.Equals(Path.GetFileName(DatabaseName), DatabaseName, StringComparison.Ordinal);
}
