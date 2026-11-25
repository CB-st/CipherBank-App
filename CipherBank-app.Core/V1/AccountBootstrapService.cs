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
    private readonly IAccountBootstrapStateStore? _stateStore;
    private readonly TimeProvider _timeProvider;
    private readonly ProductSessionAuthority? _authority;

    /// <summary>Initializes a new instance of the <see cref="AccountBootstrapService"/> class.</summary>
    public AccountBootstrapService(
        IProductClient api,
        IPrefsStore prefs,
        IRecipientRepository recipients,
        IAccountBootstrapStateStore stateStore,
        TimeProvider timeProvider,
        ProductSessionAuthority? authority)
    {
        _api = api;
        _prefs = prefs;
        _stateStore = stateStore;
        _timeProvider = timeProvider;
        _authority = authority;
    }

    /// <summary>Initializes a new instance of the <see cref="AccountBootstrapService"/> class.</summary>
    public AccountBootstrapService(IProductClient api, IPrefsStore prefs, IRecipientRepository recipients)
        : this(api, prefs, (IAccountBootstrapStateStore?)null, TimeProvider.System, null)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="AccountBootstrapService"/> class.</summary>
    public AccountBootstrapService(
        IProductClient api,
        IPrefsStore prefs,
        IRecipientRepository recipients,
        TimeProvider timeProvider)
        : this(api, prefs, (IAccountBootstrapStateStore?)null, timeProvider, null)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="AccountBootstrapService"/> class.</summary>
    public AccountBootstrapService(
        IProductClient api,
        IPrefsStore prefs,
        IRecipientRepository recipients,
        TimeProvider timeProvider,
        ProductSessionAuthority? authority)
        : this(api, prefs, (IAccountBootstrapStateStore?)null, timeProvider, authority)
    {
    }

    private AccountBootstrapService(
        IProductClient api,
        IPrefsStore prefs,
        IAccountBootstrapStateStore? stateStore,
        TimeProvider timeProvider,
        ProductSessionAuthority? authority)
    {
        _api = api;
        _prefs = prefs;
        _stateStore = stateStore;
        _timeProvider = timeProvider;
        _authority = authority;
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
        if (_stateStore is null)
        {
            throw new InvalidOperationException("Account bootstrap requires the atomic state store.");
        }

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
