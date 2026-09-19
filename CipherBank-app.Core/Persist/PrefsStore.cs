// <copyright file="PrefsStore.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Text.Json;
using CipherBank_app.Configuration;
using CipherBank_app.Persist.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CipherBank_app.Persist;

/// <inheritdoc />
public sealed class PrefsStore : IPrefsStore
{
    private const string Key = "user_prefs";
    private readonly IDbContextFactory<CipherBankDbContext> _contexts;
    private readonly UserPreferenceDefaults _defaults;

    public PrefsStore(
        IDbContextFactory<CipherBankDbContext> contexts,
        IOptions<UserPreferenceDefaultsOptions> defaults)
    {
        _contexts = contexts;
        _defaults = UserPreferenceDefaults.FromOptions(defaults.Value);
    }

    /// <inheritdoc />
    public Task<UserPrefs> LoadAsync() => LoadAsync(CancellationToken.None);

    public async Task<UserPrefs> LoadAsync(CancellationToken ct)
    {
        CipherBankDbContext context = await _contexts.CreateDbContextAsync(ct).ConfigureAwait(false);
        await using (context)
        {
            string? json = await context.Preferences
                .AsNoTracking()
                .Where(entity => entity.Key == Key)
                .Select(entity => entity.Value)
                .SingleOrDefaultAsync(ct)
                .ConfigureAwait(false);
            return DeserializePrefs(json);
        }

        UserPrefs DeserializePrefs(string? payload)
        {
            try
            {
                // Missing, blank, or malformed stored payloads converge on normalized defaults
                // (blank input throws JsonException); repository I/O and cancellation propagate.
                return payload is null
                    ? new UserPrefs(_defaults)
                    : (JsonSerializer.Deserialize<UserPrefsWireDto>(payload)?.ToPrefs(_defaults)
                        ?? new UserPrefs(_defaults));
            }
            catch (JsonException)
            {
                return new UserPrefs(_defaults);
            }
        }
    }

    /// <inheritdoc />
    public Task SaveAsync(UserPrefs prefs) => SaveAsync(prefs, CancellationToken.None);

    public Task SaveAsync(UserPrefs prefs, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(prefs);
        prefs.NormalizeHomeSections();
        return SaveCoreAsync(prefs, ct);
    }

    private async Task SaveCoreAsync(UserPrefs prefs, CancellationToken ct)
    {
        string json = JsonSerializer.Serialize(UserPrefsWireDto.FromPrefs(prefs));
        CipherBankDbContext context = await _contexts.CreateDbContextAsync(ct).ConfigureAwait(false);
        await using (context)
        {
            PreferenceEntity? entity = await context.Preferences.FindAsync([Key], ct).ConfigureAwait(false);
            if (entity is null)
            {
                context.Preferences.Add(new PreferenceEntity { Key = Key, Value = json });
            }
            else
            {
                entity.Value = json;
            }

            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }
}
