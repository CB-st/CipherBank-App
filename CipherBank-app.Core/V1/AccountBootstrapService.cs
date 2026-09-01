// <copyright file="AccountBootstrapService.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Persist;

namespace CipherBank_app.V1;

/// <inheritdoc />
public sealed class AccountBootstrapService : IAccountBootstrapService
{
    private readonly IProductClient _api;
    private readonly IPrefsStore _prefs;
    private readonly IRecipientRepository _recipients;
    private readonly TimeProvider _timeProvider;

    public AccountBootstrapService(IProductClient api, IPrefsStore prefs, IRecipientRepository recipients)
        : this(api, prefs, recipients, TimeProvider.System)
    {
    }

    public AccountBootstrapService(
        IProductClient api,
        IPrefsStore prefs,
        IRecipientRepository recipients,
        TimeProvider timeProvider)
    {
        _api = api;
        _prefs = prefs;
        _recipients = recipients;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task ApplyAsync(CancellationToken ct)
    {
        AccountBootstrapDto bootstrap = await _api.GetAccountBootstrapAsync(ct).ConfigureAwait(false);

        UserPrefs local = await _prefs.LoadAsync().ConfigureAwait(false);
        PrefsMerge.Merge(local, bootstrap.ResolvedPrefs);
        await _prefs.SaveAsync(local).ConfigureAwait(false);

        foreach (BootstrapRecipientDto contact in bootstrap.ResolvedRecipients)
        {
            string? routing = contact.ResolvedRouting;
            if (string.IsNullOrWhiteSpace(routing))
            {
                continue;
            }

            string digits = new string(routing.Where(char.IsDigit).ToArray());
            if (digits.Length != AchRecipientValidation.RoutingNumberDigitCount)
            {
                continue;
            }

            string name = contact.ResolvedName;
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            string? last4 = contact.ResolvedLast4;
            if (string.IsNullOrWhiteSpace(last4))
            {
                // Do not substitute routing trailing digits for a missing account mask.
                continue;
            }

            string accountPlaceholder = "****" + last4;
            await _recipients.UpsertAsync(new AchRecipientRow(
                contact.ResolvedId,
                name.Trim(),
                contact.ResolvedHolder,
                contact.ResolvedBank,
                digits,
                accountPlaceholder,
                contact.ResolvedAccountType is "savings" ? "savings" : "checking",
                contact.ResolvedMemo,
                AchRecipientValidation.MaskAccount(accountPlaceholder),
                AchRecipientValidation.MaskRouting(digits),
                _timeProvider.GetUtcNow())).ConfigureAwait(false);
        }
    }
}
