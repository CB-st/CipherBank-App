// <copyright file="H1H2HttpRegressionTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Net;
using System.Text;
using CipherBank_app.V1;
using Xunit;
using static CipherBank_app.Tests.Session.Regression.H1H2TestRig;

namespace CipherBank_app.Tests.Session.Regression;

public sealed class H1H2HttpRegressionTests
{
    public static TheoryData<bool, bool, bool, bool> ResponseCases
    {
        get
        {
            TheoryData<bool, bool, bool, bool> rows = [];
            foreach (bool refresh in new[] { false, true })
            {
                foreach (bool replacement in new[] { false, true })
                {
                    foreach (bool failure in new[] { false, true })
                    {
                        rows.Add(refresh, replacement, failure, false);
                        rows.Add(refresh, replacement, failure, true);
                    }
                }
            }

            return rows;
        }
    }

    [Theory]
    [MemberData(nameof(ResponseCases))]
    public async Task CreateOrRefreshResponse_AfterClearOrReplacement_CannotPublishOrRetry(
        bool refresh, bool replacement, bool failure, bool shell)
    {
        using H1H2TestRig rig = new();
        await rig.Store.SaveAsync(Credential("original"));
        AsyncGate response = new();
        int sends = 0;
        HttpProductClient old = rig.Http(
            new Transport(async (request, _) =>
            {
                if (request.RequestUri!.AbsolutePath.EndsWith("portfolio", StringComparison.Ordinal))
                {
                    Interlocked.Increment(ref sends);
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized);
                }

                await response.PauseAsync(CancellationToken.None);
                return failure ? new HttpResponseMessage(HttpStatusCode.BadRequest) : Json(Credential("stale"));
            }),
            shell);
        bool succeeded = false;
        Task attempt = CompleteAsync();
        await response.Entered;
        rig.Coordinator.StopSession();
        if (replacement)
        {
            // A second real client publishes the replacement through the same final store.
            HttpProductClient newer = rig.Http(new Transport((_, _) => Task.FromResult(Json(Credential("new")))), shell);
            await newer.CreateSessionAsync(CancellationToken.None);
        }

        response.Release();
        await ObserveAsync(() => attempt);

        Assert.Equal(replacement ? "new-access" : null, (await rig.Store.GetAsync())?.Access);
        Assert.Equal(refresh ? 1 : 0, sends);
        Assert.False(succeeded);

        async Task CompleteAsync()
        {
            if (refresh)
            {
                await old.GetPortfolioAsync(CancellationToken.None);
            }
            else
            {
                await old.CreateSessionAsync(CancellationToken.None);
            }

            succeeded = true;
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task CallerCancellation_BeforeResponseOrDuringContentMaterialization_DoesNotPublish(bool refresh, bool content)
    {
        using H1H2TestRig rig = new();
        await rig.Store.SaveAsync(Credential("original"));
        using CancellationTokenSource caller = new();
        AsyncGate gate = new();
        int sends = 0;
        HttpProductClient client = rig.Http(new Transport(async (request, ct) =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("portfolio", StringComparison.Ordinal))
            {
                Interlocked.Increment(ref sends);
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            if (content)
            {
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new GatedContent(gate) };
            }

            await gate.PauseAsync(ct);
            return Json(Credential("late"));
        }));
        Task attempt = refresh ? client.GetPortfolioAsync(caller.Token) : client.CreateSessionAsync(caller.Token);
        await gate.Entered;
        caller.Cancel();
        gate.Release();
        Exception? error = await ObserveAsync(() => attempt);

        Assert.IsAssignableFrom<OperationCanceledException>(error);
        Assert.Equal("original-access", (await rig.Store.GetAsync())?.Access);
        Assert.Equal(refresh ? 1 : 0, sends);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task SimultaneousRotatingRefresh_SameIdentity_UsesOneRefreshAndPreservesPayload(bool shell, bool cancelFirst)
    {
        using H1H2TestRig rig = new();
        await rig.Store.SaveAsync(Credential("original"));
        AsyncGate refreshGate = new();
        TaskCompletionSource bothUnauthorized = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int initial = 0;
        int refreshes = 0;
        List<string> payloads = [];
        object capture = new();
        Transport transport = new(async (request, ct) =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("refresh", StringComparison.Ordinal))
            {
                Interlocked.Increment(ref refreshes);
                await bothUnauthorized.Task.WaitAsync(ct);
                await refreshGate.PauseAsync(CancellationToken.None);
                return Json(Credential("rotated"));
            }

            string body = await request.Content!.ReadAsStringAsync(ct);
            string key = Assert.Single(request.Headers.GetValues("Idempotency-Key"));
            lock (capture)
            {
                payloads.Add(key + ":" + body);
            }

            if (request.Headers.Authorization?.Parameter == "original-access")
            {
                if (Interlocked.Increment(ref initial) == 2)
                {
                    bothUnauthorized.TrySetResult();
                }

                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            Assert.Equal("rotated-access", request.Headers.Authorization?.Parameter);
            return Json(new MoneyMoveDto());
        });
        // Separate clients, distinct transport instances, one backing store/provider.
        HttpProductClient first = rig.Http(transport, shell);
        HttpProductClient second = rig.Http(new Transport(ForwardAsync), shell);
        using CancellationTokenSource caller = new();
        Task a = first.TransferAsync("destination-a", "1", "instant", "key-a", caller.Token);
        Task b = second.TransferAsync("destination-b", "2", "instant", "key-b", CancellationToken.None);
        await bothUnauthorized.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await refreshGate.Entered;
        if (cancelFirst)
        {
            caller.Cancel();
        }

        refreshGate.Release();
        Exception? firstError = await ObserveAsync(() => a);
        await b.WaitAsync(TimeSpan.FromSeconds(10));
        if (cancelFirst)
        {
            Assert.IsAssignableFrom<OperationCanceledException>(firstError);
        }
        else
        {
            Assert.Null(firstError);
        }

        Assert.Equal(1, refreshes);
        Assert.Equal("rotated-access", (await rig.Store.GetAsync())?.Access);
        Assert.Equal(cancelFirst ? 3 : 4, payloads.Count);
        Assert.Equal(cancelFirst ? 1 : 2, payloads.Count(p => p.StartsWith("key-a:", StringComparison.Ordinal)));
        Assert.Equal(2, payloads.Count(p => p.StartsWith("key-b:", StringComparison.Ordinal)));
        Assert.Equal(2, payloads.Distinct(StringComparer.Ordinal).Count());

        // Share only the scripted boundary, never HttpMessageHandler ownership.
        Task<HttpResponseMessage> ForwardAsync(HttpRequestMessage request, CancellationToken ct)
            => transport.InvokeAsync(request, ct);
    }

    private sealed class GatedContent(AsyncGate gate) : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            => SerializeToStreamAsync(stream, context, CancellationToken.None);

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
        {
            await gate.PauseAsync(cancellationToken);
            byte[] json = Encoding.UTF8.GetBytes("{\"ACCESS_TOKEN\":\"late\",\"REFRESH_TOKEN\":\"late\",\"EXPIRES_AT\":2051226000000}");
            await stream.WriteAsync(json, cancellationToken);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}
