// <copyright file="PrefsStore.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Text.Json;
using CipherBank_app.Persist.Entities;
using Microsoft.EntityFrameworkCore;

namespace CipherBank_app.Persist;

/// <inheritdoc />
public sealed class PrefsStore : IPrefsStore
{
    private const string Key = "user_prefs";
    private readonly ILocalDb _db;

    public PrefsStore(ILocalDb db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<UserPrefs> LoadAsync()
    {
        CipherBankDbContext context = await _db.CreateContextAsync().ConfigureAwait(false);
        await using (context)
        {
            string? json = await context.Preferences
                .AsNoTracking()
                .Where(entity => entity.Key == Key)
                .Select(entity => entity.Value)
                .SingleOrDefaultAsync()
                .ConfigureAwait(false);
            UserPrefs prefs = DeserializePrefs(json);
            prefs.NormalizeHomeSections();
            return prefs;
        }

        static UserPrefs DeserializePrefs(string? payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return new UserPrefs();
            }

            try
            {
                return JsonSerializer.Deserialize<UserPrefs>(payload) ?? new UserPrefs();
            }
            catch (JsonException)
            {
                return new UserPrefs();
            }
        }
    }

    /// <inheritdoc />
    public Task SaveAsync(UserPrefs prefs)
    {
        ArgumentNullException.ThrowIfNull(prefs);
        prefs.NormalizeHomeSections();
        return SaveCoreAsync(prefs);
    }

    private async Task SaveCoreAsync(UserPrefs prefs)
    {
        string json = JsonSerializer.Serialize(prefs);
        CipherBankDbContext context = await _db.CreateContextAsync().ConfigureAwait(false);
        await using (context)
        {
            PreferenceEntity? entity = await context.Preferences.FindAsync(Key).ConfigureAwait(false);
            if (entity is null)
            {
                context.Preferences.Add(new PreferenceEntity { Key = Key, Value = json });
            }
            else
            {
                entity.Value = json;
            }

            await context.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
