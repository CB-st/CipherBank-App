// <copyright file="AccountBootstrapService.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Persist;

namespace CipherBank_app.V1;

/// <inheritdoc />
public sealed class AccountBootstrapService(
    IProductClient api,
    IPrefsStore prefs,
    IAccountBootstrapStateStore stateStore,
    TimeProvider timeProvider,
    ProductSessionAuthority? authority) : IAccountBootstrapService
{
    private readonly IProductClient _api = api;
    private readonly IPrefsStore _prefs = prefs;
    private readonly IAccountBootstrapStateStore _stateStore = stateStore;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ProductSessionAuthority? _authority = authority;

    /// <summary>Initializes a new instance of the <see cref="AccountBootstrapService"/> class.</summary>
    public AccountBootstrapService(IProductClient api, IPrefsStore prefs, IAccountBootstrapStateStore stateStore)
        : this(api, prefs, stateStore, TimeProvider.System, null)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="AccountBootstrapService"/> class.</summary>
    public AccountBootstrapService(
        IProductClient api,
        IPrefsStore prefs,
        IAccountBootstrapStateStore stateStore,
        TimeProvider timeProvider)
        : this(api, prefs, stateStore, timeProvider, null)
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
        List<AchRecipientRow> rows = BuildRecipients(bootstrap.ResolvedRecipients);
        PrefsMerge.Merge(local, bootstrap.ResolvedPrefs);
        await _stateStore.CommitAsync(local, rows, ct).ConfigureAwait(false);
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
}
