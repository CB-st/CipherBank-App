// <copyright file="UserDataServiceCollectionExtensionsTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Persist;
using CipherBank_app.UserData;
using CipherBank_app.V1;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CipherBank_app.Tests.UserData;

public class UserDataServiceCollectionExtensionsTests
{
    [Fact]
    public void AddUserDataPrefsSync_ResolvesPackBackedIPrefsSyncService()
    {
        ServiceCollection services = new();
        services.AddSingleton<IPrefsStore, MemPrefs>();
        services.AddSingleton<IProductApi, MockProductApi>();
        services.AddUserDataPrefsSync();

        using ServiceProvider sp = services.BuildServiceProvider();
        IPrefsSyncService sync = sp.GetRequiredService<IPrefsSyncService>();
        sync.Should().BeOfType<UserDataPrefsSyncService>();
        sp.GetRequiredService<IUserDataClient>().Should().BeOfType<MockUserDataClient>();
        sp.GetRequiredService<MutableUserDataAccountContext>().Should().NotBeNull();
    }

    private sealed class MemPrefs : IPrefsStore
    {
        public Task<UserPrefs> LoadAsync() => Task.FromResult(new UserPrefs());

        public Task SaveAsync(UserPrefs prefs) => Task.CompletedTask;
    }
}
