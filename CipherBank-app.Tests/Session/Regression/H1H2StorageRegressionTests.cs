// <copyright file="H1H2StorageRegressionTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Net;
using CipherBank_app.V1;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static CipherBank_app.Tests.Session.Regression.H1H2TestRig;

namespace CipherBank_app.Tests.Session.Regression;

public sealed class H1H2StorageRegressionTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task PartialSecureWrite_ClearTwiceAndRelease_RevokesAndAllowsReplacement(bool fail, bool shell)
    {
        H1H2ThreeKeyStore backing = new();
        using H1H2TestRig rig = new(backing);
        Assert.Same(backing, rig.Services.GetRequiredService<IProductSessionStore>());
        AsyncGate gate = new();
        backing.SaveGate = gate;
        backing.FailAfterFirstWrite = fail;
        HttpProductClient old = rig.Http(new Transport((_, _) => Task.FromResult(Json(Credential("old")))), shell);
        bool succeeded = false;
        Task attempt = CompleteAsync();
        await gate.Entered;
        rig.Coordinator.StopSession();
        rig.Coordinator.StopSession();
        gate.Release();
        await ObserveAsync(() => attempt);

        Assert.False(succeeded);
        string? attached = null;
        HttpProductClient reader = rig.Http(
            new Transport((request, _) =>
            {
                attached = request.Headers.Authorization?.Parameter;
                return Task.FromResult(Json(new PortfolioDto()));
            }),
            shell);
        await ObserveAsync(() => reader.GetPortfolioAsync(CancellationToken.None));
        Assert.Null(attached);
        backing.SaveGate = null;
        backing.FailAfterFirstWrite = false;
        HttpProductClient replacement = rig.Http(new Transport((_, _) => Task.FromResult(Json(Credential("new")))), shell);
        await replacement.CreateSessionAsync(CancellationToken.None);
        Assert.Equal("new-access", (await backing.GetAsync())?.Access);
        Assert.Equal(3, backing.KeyCount);

        async Task CompleteAsync()
        {
            await old.CreateSessionAsync(CancellationToken.None);
            succeeded = true;
        }
    }

    [Fact]
    public async Task PartialSecureWriteFailure_IsObservableAndDoesNotPoisonReplacement()
    {
        H1H2ThreeKeyStore backing = new() { FailAfterFirstWrite = true };
        using H1H2TestRig rig = new(backing);
        HttpProductClient client = rig.Http(new Transport((_, _) => Task.FromResult(Json(Credential("broken")))));
        Exception? failure = await ObserveAsync(() => client.CreateSessionAsync(CancellationToken.None));
        Assert.IsType<InvalidOperationException>(failure);
        Assert.Null(await backing.GetAsync());
        backing.FailAfterFirstWrite = false;
        await client.CreateSessionAsync(CancellationToken.None);
        Assert.Equal("broken-access", (await backing.GetAsync())?.Access);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BearerInitialRead_AcrossClearAndReplacement_NeverAttachesOldAccess(bool shell)
    {
        H1H2ThreeKeyStore backing = new();
        await backing.SaveAsync(Credential("old"));
        using H1H2TestRig rig = new(backing);
        AsyncGate read = new();
        backing.ReadGate = read;
        List<string?> attached = [];
        HttpProductClient client = rig.Http(
            new Transport((request, _) =>
            {
                attached.Add(request.Headers.Authorization?.Parameter);
                return Task.FromResult(Json(new PortfolioDto()));
            }),
            shell);
        Task attempt = client.GetPortfolioAsync(CancellationToken.None);
        await read.Entered;
        rig.Coordinator.StopSession();
        backing.ReadGate = null;
        read.Release();
        await ObserveAsync(() => attempt);

        Assert.DoesNotContain("old-access", attached);
        Assert.Null(await backing.GetAsync());
        HttpProductClient replacement = rig.Http(new Transport((_, _) => Task.FromResult(Json(Credential("new")))), shell);
        await replacement.CreateSessionAsync(CancellationToken.None);
        Assert.Equal("new-access", (await backing.GetAsync())?.Access);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Cancellation_DuringAdmittedPersistence_CannotReturnSessionSuccess(bool refresh)
    {
        H1H2ThreeKeyStore backing = new();
        await backing.SaveAsync(Credential("original"));
        using H1H2TestRig rig = new(backing);
        using CancellationTokenSource caller = new();
        AsyncGate save = new();
        backing.SaveGate = save;
        int sends = 0;
        HttpProductClient client = rig.Http(new Transport((request, _) =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("portfolio", StringComparison.Ordinal))
            {
                Interlocked.Increment(ref sends);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
            }

            return Task.FromResult(Json(Credential("cancelled")));
        }));
        Task attempt = refresh ? client.GetPortfolioAsync(caller.Token) : client.CreateSessionAsync(caller.Token);
        await save.Entered;
        caller.Cancel();
        save.Release();
        Exception? error = await ObserveAsync(() => attempt);
        Assert.IsAssignableFrom<OperationCanceledException>(error);
        Assert.Equal(refresh ? 1 : 0, sends);
        // An admitted physical write may drain later. Readability is owned by the bearer authority.
        foreach (bool shell in new[] { false, true })
        {
            List<string?> attached = [];
            HttpProductClient reader = rig.Http(
                new Transport((request, _) =>
                {
                    attached.Add(request.Headers.Authorization?.Parameter);
                    return Task.FromResult(Json(new PortfolioDto()));
                }),
                shell);
            await ObserveAsync(() => reader.GetPortfolioAsync(CancellationToken.None));
            Assert.DoesNotContain("cancelled-access", attached);
        }

        backing.SaveGate = null;
        HttpProductClient replacement = rig.Http(new Transport((_, _) => Task.FromResult(Json(Credential("replacement")))));
        await replacement.CreateSessionAsync(CancellationToken.None);
        Assert.Equal("replacement-access", (await backing.GetAsync())?.Access);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LateHostStore_PositiveCreateRefreshAndProviderIsolation(bool shell)
    {
        H1H2ThreeKeyStore backing = new();
        using H1H2TestRig rig = new(backing);
        using H1H2TestRig independent = new(new H1H2ThreeKeyStore());
        await independent.Store.SaveAsync(Credential("independent"));
        int refreshes = 0;
        Transport transport = new((request, _) =>
        {
            string path = request.RequestUri!.AbsolutePath;
            if (path.EndsWith("refresh", StringComparison.Ordinal))
            {
                Interlocked.Increment(ref refreshes);
                return Task.FromResult(Json(Credential("rotated")));
            }

            if (path.EndsWith("session", StringComparison.Ordinal))
            {
                return Task.FromResult(Json(Credential("created")));
            }

            return Task.FromResult(request.Headers.Authorization?.Parameter == "created-access"
                ? new HttpResponseMessage(HttpStatusCode.Unauthorized) : Json(new PortfolioDto()));
        });
        HttpProductClient client = rig.Http(transport, shell);
        HttpProductClient peer = rig.Http(new Transport(transport.InvokeAsync), !shell);
        await client.CreateSessionAsync(CancellationToken.None);
        Assert.Equal("created-access", (await backing.GetAsync())?.Access);
        await peer.GetPortfolioAsync(CancellationToken.None);
        Assert.Equal(1, refreshes);
        Assert.Equal("rotated-access", (await backing.GetAsync())?.Access);
        rig.Coordinator.StopSession();
        Assert.Null(await backing.GetAsync());
        Assert.Equal("independent-access", (await independent.Store.GetAsync())?.Access);
        Assert.True(await rig.App.UnlockAsync("123456"));
        Assert.Equal("current-access", (await backing.GetAsync())?.Access);
        rig.App.Lock();
        Assert.True(await rig.App.UnlockAsync("123456"));
        Assert.Equal("current-access", rig.App.AccessToken);
    }
}
