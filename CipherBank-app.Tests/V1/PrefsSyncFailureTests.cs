// <copyright file="PrefsSyncFailureTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Reflection;
using System.Text.Json;
using CipherBank_app.Persist;
using CipherBank_app.Tests.Persist;
using CipherBank_app.V1;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.V1;

/// <summary>Push failures keep the local save and report false instead of throwing.</summary>
public sealed class PrefsSyncFailureTests
{
    public static TheoryData<Type> PushFailures => new()
    {
        typeof(HttpRequestException),
        typeof(TaskCanceledException),
        typeof(JsonException),
        typeof(InvalidOperationException),
        typeof(ArgumentException),
    };

    [Theory]
    [MemberData(nameof(PushFailures))]
    public async Task SaveAndPush_PushFails_SavesLocallyAndReturnsFalse(Type failure)
    {
        MemPrefs store = new();
        IProductClient api = ThrowingPrefsClient.Create((Exception)Activator.CreateInstance(failure)!);
        PrefsSyncService sync = new(store, api);
        UserPrefs prefs = new(TestPreferenceDefaults.Value) { AssetsLayout = "combined" };

        bool pushed = await sync.SaveAndPushAsync(prefs, default);

        pushed.Should().BeFalse();
        store.Current.Should().BeSameAs(prefs);
    }

    [Fact]
    public async Task SaveAndPush_NullPrefs_Throws()
    {
        PrefsSyncService sync = new(new MemPrefs(), new InMemoryProductClient());

        Func<Task> act = () => sync.SaveAndPushAsync(null!, default);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task InMemoryClient_ChallengeAndKeyShare_ReturnMockShapes()
    {
        InMemoryProductClient api = new();

        SessionChallengeDto challenge = await api.CreateSessionChallengeAsync("pk", default);
        KeyShareResponseDto share = await api.EstablishKeyShareAsync(new KeyShareRequestDto(), default);

        challenge.ChallengeId.Should().StartWith("ch_mock_");
        challenge.ApiKeyId.Should().Be("api_mock");
        challenge.Algorithm.Should().Be("x25519-chacha20poly1305");
        Convert.FromBase64String(challenge.Ciphertext).Should().NotBeEmpty();
        share.KeyShareId.Should().StartWith("ks_mock_");
        share.Algorithm.Should().Be("hybrid-mlkem768-x25519-v1");
        Convert.FromBase64String(share.MlKemCiphertext).Should().NotBeEmpty();
    }

    /// <summary>Product client whose PutPrefsAsync throws; other members are unused.</summary>
    public class ThrowingPrefsClient : DispatchProxy
    {
        private Exception _failure = new InvalidOperationException();

        public static IProductClient Create(Exception failure)
        {
            IProductClient proxy = Create<IProductClient, ThrowingPrefsClient>();
            ((ThrowingPrefsClient)(object)proxy)._failure = failure;
            return proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(IProductClient.PutPrefsAsync))
            {
                return Task.FromException(_failure);
            }

            throw new NotSupportedException(targetMethod?.Name);
        }
    }

    private sealed class MemPrefs : IPrefsStore
    {
        public UserPrefs Current { get; set; } = new(TestPreferenceDefaults.Value);

        public Task<UserPrefs> LoadAsync(CancellationToken ct = default) => Task.FromResult(Current);

        public Task SaveAsync(UserPrefs prefs, CancellationToken ct = default)
        {
            Current = prefs;
            return Task.CompletedTask;
        }
    }
}
