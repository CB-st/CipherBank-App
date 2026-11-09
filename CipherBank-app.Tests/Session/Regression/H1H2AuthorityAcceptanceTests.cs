// <copyright file="H1H2AuthorityAcceptanceTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Reflection;
using CipherBank_app.Configuration;
using CipherBank_app.Persist;
using CipherBank_app.V1;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using static CipherBank_app.Tests.Session.Regression.H1H2TestRig;

namespace CipherBank_app.Tests.Session.Regression;

public sealed class H1H2AuthorityAcceptanceTests
{
    [Fact]
    public void CoreDefault_ThenLateHostOverride_ResolvesOneFinalAuthority()
    {
        H1H2ThreeKeyStore backing = new();
        using H1H2TestRig rig = new(backing);
        using H1H2TestRig independent = new(new H1H2ThreeKeyStore());
        ProductSessionAuthority authority = rig.Services.GetRequiredService<ProductSessionAuthority>();
        Assert.Same(ProductSessionAuthority.For(backing), authority);
        Assert.Same(authority, rig.Services.GetRequiredService<ProductSessionAuthority>());
        Assert.NotSame(authority, independent.Services.GetRequiredService<ProductSessionAuthority>());
        // The existing late-host positives exercise two clients, both handlers, coordinator and lock/reunlock.
        Assert.Same(backing, rig.Services.GetRequiredService<IProductSessionStore>());
    }

    [Fact]
    public void TransientFinalBacking_IsRejectedBeforeAnyUse()
    {
        ServiceCollection services = new();
        services.AddProductSessionServices();
        services.AddTransient<IProductSessionStore, InMemoryProductSessionStore>();
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<ProductSessionAuthority>());
    }

    [Fact]
    public async Task B1_NestedInvalidation_PreservesEpochAndObservedDrain()
    {
        H1H2ThreeKeyStore backing = new();
        using ProductSessionAuthority authority = ProductSessionAuthority.For(backing);
        await authority.PublishAsync(authority.Begin(), Credential("old"), CancellationToken.None);
        Task? observed = null;
        ProductSessionOperation? revoked = null;
        authority.Invalidate(onRevoked: () =>
        {
            observed = authority.Drain;
            revoked = authority.Capture();
            Assert.False(observed.IsCompleted);
            authority.Invalidate();
            Assert.Same(observed, authority.Drain);
            authority.Check(revoked);
        });
        Assert.NotNull(observed);
        Assert.Same(observed, authority.Drain);
        await observed;
        Assert.Equal(1, backing.ClearCalls);
        await authority.PublishAsync(authority.Begin(), Credential("replacement"), CancellationToken.None);
        Assert.Equal("replacement-access", (await backing.GetAsync())?.Access);
        Assert.Equal(3, backing.KeyCount);
        Assert.Equal(1, backing.ClearCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InitialRead_RevokesImmediately_ThenDrainsPhysicalClear(bool betweenKeys)
    {
        H1H2ThreeKeyStore backing = new();
        await backing.SaveAsync(Credential("old"));
        ProductSessionAuthority authority = ProductSessionAuthority.For(backing);
        AsyncGate read = new();
        if (betweenKeys)
        {
            backing.ReadFirstKeyGate = read;
        }
        else
        {
            backing.ReadGate = read;
        }

        Task old = authority.ReadAsync(authority.Capture());
        await read.Entered;
        authority.Invalidate();
        Assert.Null(await authority.ReadAsync(authority.Capture()));
        Assert.False(authority.Drain.IsCompleted);
        read.Release();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => old);
        await authority.Drain;
        backing.ReadGate = null;
        backing.ReadFirstKeyGate = null;
        Assert.Equal(0, backing.KeyCount);
        Assert.Null(await backing.GetAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PartialWrite_RevokesImmediately_ReplacementWaitsForOldDrain(bool partialError)
    {
        H1H2ThreeKeyStore backing = new();
        ProductSessionAuthority authority = ProductSessionAuthority.For(backing);
        AsyncGate save = new();
        backing.SaveGate = save;
        backing.FailAfterFirstWrite = partialError;
        Task old = authority.PublishAsync(authority.Begin(), Credential("old"), CancellationToken.None);
        await save.Entered;
        authority.Invalidate();
        authority.Invalidate();
        Assert.Null(await authority.ReadAsync(authority.Capture()));
        Assert.Equal(1, backing.KeyCount);
        Task replacement = authority.PublishAsync(authority.Begin(), Credential("new"), CancellationToken.None);
        Assert.False(replacement.IsCompleted);
        backing.SaveGate = null;
        // Error setting is sampled after the gate; separate error/recovery coverage below preserves the failed-write case.
        if (!partialError)
        {
            backing.FailAfterFirstWrite = false;
        }

        save.Release();
        await ObserveAsync(() => old);
        if (partialError)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => replacement);
            backing.FailAfterFirstWrite = false;
            await authority.PublishAsync(authority.Begin(), Credential("new"), CancellationToken.None);
        }
        else
        {
            await replacement;
        }

        await authority.Drain;
        Assert.Equal("new-access", (await backing.GetAsync())?.Access);
        Assert.Equal(3, backing.KeyCount);
    }

    [Fact]
    public async Task CleanupFailure_IsObservable_BlocksPublication_AndRepeatedInvalidationRecovers()
    {
        FailingCleanupStore backing = new();
        await backing.SaveAsync(Credential("old"));
        ProductSessionAuthority authority = ProductSessionAuthority.For(backing);
        backing.FailClear = true;
        authority.Invalidate();
        await Assert.ThrowsAsync<InvalidOperationException>(() => authority.Drain);
        Assert.Null(await authority.ReadAsync(authority.Capture()));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            authority.PublishAsync(authority.Begin(), Credential("blocked"), CancellationToken.None));
        Assert.Equal("old-access", (await backing.GetAsync())?.Access);
        backing.FailClear = false;
        authority.Invalidate();
        await authority.Drain;
        Assert.Null(await backing.GetAsync());
        await authority.PublishAsync(authority.Begin(), Credential("recovered"), CancellationToken.None);
        Assert.Equal("recovered-access", (await backing.GetAsync())?.Access);
    }

    [Fact]
    public async Task EpochAndCredentialRevision_AreIndependent_AndOldRevisionCannotPublish()
    {
        InMemoryProductSessionStore backing = new();
        ProductSessionAuthority authority = ProductSessionAuthority.For(backing);
        ProductSessionOperation operation = authority.Begin();
        ProductSessionSnapshot original = await authority.PublishAsync(operation, Credential("first"), CancellationToken.None);
        ProductSessionSnapshot rotated = await authority.PublishAsync(operation, Credential("rotated"), original.Revision, CancellationToken.None);
        Assert.Same(operation, rotated.Operation);
        Assert.True(rotated.Revision > original.Revision);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            authority.PublishAsync(operation, Credential("stale"), original.Revision, CancellationToken.None));
        Assert.Equal("rotated-access", (await backing.GetAsync())?.Access);
        authority.Invalidate();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            authority.PublishAsync(operation, Credential("revoked"), CancellationToken.None));
        Assert.Null(await backing.GetAsync());
    }

    [Fact]
    public void PublishAsync_PublicOverloads_TakeCancellationTokenLast()
    {
        MethodInfo[] overloads = typeof(ProductSessionAuthority)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Where(method => method.Name == nameof(ProductSessionAuthority.PublishAsync))
            .ToArray();
        Assert.NotEmpty(overloads);
        foreach (MethodInfo overload in overloads)
        {
            ParameterInfo[] parameters = overload.GetParameters();
            Assert.NotEmpty(parameters);
            Assert.Equal(typeof(CancellationToken), parameters[^1].ParameterType);
        }

        MethodInfo revisionOverload = Assert.Single(overloads, method =>
            method.GetParameters().Any(parameter => parameter.ParameterType == typeof(long?)));
        ParameterInfo[] revisionParameters = revisionOverload.GetParameters();
        ParameterInfo expectedRevision = Assert.Single(revisionParameters, parameter => parameter.ParameterType == typeof(long?));
        Assert.Equal("expectedRevision", expectedRevision.Name);
        Assert.Equal(revisionParameters.Length - 2, expectedRevision.Position);
    }

    [Fact]
    public async Task PublishAsync_WithExpectedRevision_HonorsCancellationTokenInLastPosition()
    {
        InMemoryProductSessionStore backing = new();
        using ProductSessionAuthority authority = ProductSessionAuthority.For(backing);
        ProductSessionOperation operation = authority.Begin();
        ProductSessionSnapshot original = await authority.PublishAsync(operation, Credential("first"), CancellationToken.None);
        (string Access, string Refresh, DateTimeOffset Expires)? stored = await backing.GetAsync();
        CancellationToken cancelled = new(canceled: true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            authority.PublishAsync(operation, Credential("cancelled"), original.Revision, cancelled));
        Assert.Equal(stored, await backing.GetAsync());
        Assert.Equal(original, await authority.ReadAsync(operation));

        ProductSessionSnapshot published = await authority.PublishAsync(
            operation, Credential("replacement"), original.Revision, CancellationToken.None);
        Assert.Equal(original.Revision + 1, published.Revision);
        Assert.Equal("replacement-access", published.Access);
        Assert.Equal("replacement-refresh", published.Refresh);
        Assert.Equal(published, await authority.ReadAsync(operation));
        Assert.Equal((published.Access, published.Refresh, published.Expires), await backing.GetAsync());
    }

    [Fact]
    public async Task DifferentAuthorities_WithEqualEpochs_RejectForeignIdentity()
    {
        ProductSessionAuthority first = ProductSessionAuthority.For(new InMemoryProductSessionStore());
        ProductSessionAuthority second = ProductSessionAuthority.For(new InMemoryProductSessionStore());
        ProductSessionOperation foreign = first.Begin();
        ProductSessionOperation local = second.Begin();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            second.PublishAsync(foreign, Credential("foreign"), CancellationToken.None));
        await second.PublishAsync(local, Credential("local"), CancellationToken.None);
        second.Invalidate(foreign);
        Assert.Equal("local-access", (await second.ReadAsync(local))?.Access);
    }

    [Fact]
    public async Task AdmittedLocalWrite_DrainsBeforeReplacementPersistence()
    {
        InMemoryProductSessionStore backing = new();
        ProductSessionAuthority authority = ProductSessionAuthority.For(backing);
        AsyncGate write = new();
        ProductSessionOperation oldIdentity = authority.Begin();
        Task old = authority.RunLocalAsync(oldIdentity, () => write.PauseAsync(), CancellationToken.None);
        await write.Entered;
        authority.Invalidate();
        Task replacement = authority.PublishAsync(authority.Begin(), Credential("replacement"), CancellationToken.None);
        Assert.False(replacement.IsCompleted);
        write.Release();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => old);
        await replacement;
        Assert.Equal("replacement-access", (await backing.GetAsync())?.Access);
    }

    [Fact]
    public async Task RawLegacyBypass_IsExplicitlyOutsideAuthorityContract()
    {
        // A raw implementation has no notification API. Do not teach the faithful fake lifecycle policy.
        H1H2ThreeKeyStore backing = new();
        ProductSessionAuthority authority = ProductSessionAuthority.For(backing);
        authority.Invalidate();
        await authority.Drain;
        await backing.SaveAsync(Credential("external-bypass"));
        Assert.Null(await authority.ReadAsync(authority.Capture()));
        Assert.Equal(3, backing.KeyCount); // Physical safety requires adapting every concrete raw writer.
        authority.Invalidate();
        await authority.Drain;
        Assert.Equal(0, backing.KeyCount);
    }

    [Fact]
    public async Task RealPrefsPull_InvalidatedRemoteCompletion_CannotAdmitLocalWrite()
    {
        InMemoryProductSessionStore backing = new();
        ProductSessionAuthority authority = ProductSessionAuthority.For(backing);
        Mock<IPrefsStore> prefs = new();
        prefs.Setup(p => p.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserPrefs(CipherBank_app.Tests.Persist.TestPreferenceDefaults.Value));
        Mock<IProductClient> api = new();
        AsyncGate response = new();
        api.Setup(p => p.GetPrefsAsync(It.IsAny<CancellationToken>())).Returns(async () =>
        {
            await response.PauseAsync();
            return new PrefsWireDto();
        });
        PrefsSyncService sync = new(prefs.Object, api.Object, authority);
        Task pull = sync.PullMergeAsync(CancellationToken.None);
        await response.Entered;
        authority.Invalidate();
        response.Release();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pull);
        prefs.Verify(p => p.SaveAsync(It.IsAny<UserPrefs>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RealBootstrap_InvalidatedRemoteCompletion_CannotAdmitPrefsOrRecipientWrites()
    {
        ProductSessionAuthority authority = ProductSessionAuthority.For(new InMemoryProductSessionStore());
        Mock<IPrefsStore> prefs = new();
        prefs.Setup(p => p.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserPrefs(CipherBank_app.Tests.Persist.TestPreferenceDefaults.Value));
        Mock<IRecipientRepository> recipients = new();
        Mock<IProductClient> api = new();
        AsyncGate response = new();
        api.Setup(p => p.GetAccountBootstrapAsync(It.IsAny<CancellationToken>())).Returns(async () =>
        {
            await response.PauseAsync();
            return new AccountBootstrapDto();
        });
        AccountBootstrapService bootstrap = new(api.Object, prefs.Object, recipients.Object, new FixedClock(), authority);
        Task apply = bootstrap.ApplyAsync(CancellationToken.None);
        await response.Entered;
        authority.Invalidate();
        response.Release();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => apply);
        prefs.Verify(p => p.SaveAsync(It.IsAny<UserPrefs>(), It.IsAny<CancellationToken>()), Times.Never);
        recipients.Verify(p => p.UpsertAsync(It.IsAny<AchRecipientRow>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed class FailingCleanupStore : IProductSessionStore
    {
        private readonly InMemoryProductSessionStore _inner = new();

        internal bool FailClear { get; set; }

        public Task SaveAsync(SessionDto session) => _inner.SaveAsync(session);

        public Task<(string Access, string Refresh, DateTimeOffset Expires)?> GetAsync() => _inner.GetAsync();

        public void Clear()
        {
            if (FailClear)
            {
                throw new InvalidOperationException("fixture cleanup failure");
            }

            _inner.Clear();
        }
    }
}
