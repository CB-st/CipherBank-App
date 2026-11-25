// <copyright file="AccountBootstrapService.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Persist;

namespace CipherBank_app.V1;

/// <inheritdoc />
public sealed class AccountBootstrapService(
    IProductClient api,
    IPrefsStore prefs,
    IRecipientRepository recipients,
    TimeProvider timeProvider,
    ProductSessionAuthority? authority) : IAccountBootstrapService
{
    private readonly IProductClient _api = api;
    private readonly IPrefsStore _prefs = prefs;
    private readonly IRecipientRepository _recipients = recipients;
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly ProductSessionAuthority? _authority = authority;

    public AccountBootstrapService(IProductClient api, IPrefsStore prefs, IRecipientRepository recipients)
        : this(api, prefs, recipients, TimeProvider.System)
    {
    }

    public AccountBootstrapService(
        IProductClient api,
        IPrefsStore prefs,
        IRecipientRepository recipients,
        TimeProvider timeProvider)
        : this(api, prefs, recipients, timeProvider, null)
    {
    }

    public Task ApplyAsync(CancellationToken ct)
        => ApplyAsync(_authority?.Capture(), _authority, ct);

    internal async Task ApplyAsync(ProductSessionOperation? operation, ProductSessionAuthority? authority, CancellationToken ct)
    {
        authority ??= _authority;
        AccountBootstrapDto bootstrap = await _api.GetAccountBootstrapAsync(ct).ConfigureAwait(false);
        await WriteAsync(
            operation,
            () => ApplyLocalAsync(bootstrap, ct),
            authority,
            ct).ConfigureAwait(false);
    }

    private static async Task WriteAsync(ProductSessionOperation? operation, Func<Task> action, ProductSessionAuthority? authority, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (authority is not null && operation is not null)
        {
            await authority.RunLocalAsync(operation, action, ct).ConfigureAwait(false);
        }
        else
        {
            await action().ConfigureAwait(false);
        }
    }

    private async Task ApplyLocalAsync(AccountBootstrapDto bootstrap, CancellationToken ct)
    {
        UserPrefs local = await _prefs.LoadAsync(ct).ConfigureAwait(false);
        IReadOnlyList<AchRecipientRow> existingRecipients = [.. await _recipients.ListAsync(ct).ConfigureAwait(false)];
        UserPrefsSnapshot prefsSnapshot = new(local);
        List<AchRecipientRow> rows = BuildRecipients(bootstrap.ResolvedRecipients);
        List<string> attemptedIds = [];

        try
        {
            PrefsMerge.Merge(local, bootstrap.ResolvedPrefs);
            await _prefs.SaveAsync(local, ct).ConfigureAwait(false);
            foreach (AchRecipientRow row in rows)
            {
                attemptedIds.Add(row.Id);
                await _recipients.UpsertAsync(row, ct).ConfigureAwait(false);
            }
        }
        catch
        {
            await RestoreAsync(local, prefsSnapshot, existingRecipients, attemptedIds).ConfigureAwait(false);
            throw;
        }
    }

    private List<AchRecipientRow> BuildRecipients(IReadOnlyList<BootstrapRecipientDto> contacts)
    {
        List<AchRecipientRow> rows = [];
        foreach (BootstrapRecipientDto contact in contacts)
        {
            string? routing = contact.ResolvedRouting;
            if (string.IsNullOrWhiteSpace(routing))
            {
                continue;
            }

            string digits = new([.. routing.Where(char.IsDigit)]);
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
            rows.Add(new AchRecipientRow(
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
                _timeProvider.GetUtcNow()));
        }

        return rows;
    }

    private async Task RestoreAsync(
        UserPrefs local,
        UserPrefsSnapshot prefsSnapshot,
        IEnumerable<AchRecipientRow> existingRecipients,
        IEnumerable<string> attemptedIds)
    {
        prefsSnapshot.Restore(local);
        await _prefs.SaveAsync(local, CancellationToken.None).ConfigureAwait(false);
        foreach (string id in attemptedIds.Distinct(StringComparer.Ordinal))
        {
            AchRecipientRow? previous = existingRecipients.FirstOrDefault(row => row.Id == id);
            if (previous is null)
            {
                await _recipients.DeleteAsync(id, CancellationToken.None).ConfigureAwait(false);
            }
            else
            {
                await _recipients.UpsertAsync(previous, CancellationToken.None).ConfigureAwait(false);
            }
        }
    }

    private sealed class UserPrefsSnapshot(UserPrefs source)
    {
        private readonly string[] _homeOrder = [.. source.HomeOrder];
        private readonly Dictionary<string, bool> _homeVisible = new(source.HomeVisible, StringComparer.Ordinal);
        private readonly string _assetsLayout = source.AssetsLayout;
        private readonly bool _valuesHiddenOnLaunch = source.ValuesHiddenOnLaunch;
        private readonly bool _coraEnabled = source.CoraEnabled;
        private readonly string _defaultSendSpeed = source.DefaultSendSpeed;
        private readonly string _appearance = source.Appearance;
        private readonly string _baseCurrency = source.BaseCurrency;
        private readonly string[] _enabledCurrencies = [.. source.EnabledCurrencies];
        private readonly int _lockIdleSeconds = source.LockIdleSeconds;

        public void Restore(UserPrefs target)
        {
            target.ReplaceHomeOrder(_homeOrder);
            target.ReplaceHomeVisible(_homeVisible);
            target.AssetsLayout = _assetsLayout;
            target.ValuesHiddenOnLaunch = _valuesHiddenOnLaunch;
            target.CoraEnabled = _coraEnabled;
            target.DefaultSendSpeed = _defaultSendSpeed;
            target.Appearance = _appearance;
            target.BaseCurrency = _baseCurrency;
            target.ReplaceEnabledCurrencies(_enabledCurrencies);
            target.LockIdleSeconds = _lockIdleSeconds;
        }
    }
}
