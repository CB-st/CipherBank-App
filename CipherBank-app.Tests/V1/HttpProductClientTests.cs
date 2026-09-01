// <copyright file="HttpProductClientTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Net;
using System.Text;
using CipherBank_app.V1;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.V1;

public sealed class HttpProductClientTests
{
    [Fact]
    public async Task GetPortfolioAsync_DeserializesSnapshot()
    {
        StubHandler handler = new StubHandler(_ => JsonResponse(
            """{"TOTAL_USD":"10.00","CHANGE_24H_PCT":"1.2","HOLDINGS":[]}"""));
        HttpProductClient client = CreateClient(handler);

        PortfolioDto portfolio = await client.GetPortfolioAsync(CancellationToken.None);

        portfolio.TotalUsd.Should().Be("10.00");
        handler.Requests.Should().ContainSingle(r => r.Method == HttpMethod.Get && r.Path == "v1/portfolio");
    }

    [Fact]
    public async Task CreateSessionAsync_SavesTokensFromResponse()
    {
        StubHandler handler = new StubHandler(_ => JsonResponse(
            """{"ACCESS_TOKEN":"access-1","REFRESH_TOKEN":"refresh-1","EXPIRES_AT":1720900000000}"""));
        InMemoryProductSessionStore sessions = new InMemoryProductSessionStore();
        HttpProductClient client = CreateClient(handler, sessions);

        SessionDto session = await client.CreateSessionAsync(CancellationToken.None);

        session.AccessToken.Should().Be("access-1");
        (await sessions.GetAsync()).Should().NotBeNull();
        (await sessions.GetAsync())!.Value.Access.Should().Be("access-1");
    }

    [Fact]
    public async Task GetAsync_RefreshesOnce_OnUnauthorized()
    {
        int portfolioCalls = 0;
        StubHandler handler = new StubHandler(req =>
        {
            if (req.Path == "v1/session/refresh")
            {
                return JsonResponse(
                    """{"ACCESS_TOKEN":"access-2","REFRESH_TOKEN":"refresh-2","EXPIRES_AT":1720900001000}""");
            }

            if (req.Path == "v1/portfolio")
            {
                portfolioCalls++;
                if (portfolioCalls == 1)
                {
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized);
                }

                return JsonResponse("""{"TOTAL_USD":"1","CHANGE_24H_PCT":"0","HOLDINGS":[]}""");
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        InMemoryProductSessionStore sessions = new InMemoryProductSessionStore();
        await sessions.SaveAsync(new SessionDto
        {
            AccessToken = "access-1",
            RefreshToken = "refresh-1",
            ExpiresAt = 1,
        });
        HttpProductClient client = CreateClient(handler, sessions);

        PortfolioDto portfolio = await client.GetPortfolioAsync(CancellationToken.None);

        portfolio.TotalUsd.Should().Be("1");
        portfolioCalls.Should().Be(2);
        (await sessions.GetAsync())!.Value.Access.Should().Be("access-2");
        handler.Requests.Should().Contain(r => r.Path == "v1/session/refresh");
    }

    [Fact]
    public async Task GetPrefsAsync_ReturnsNull_OnHttpFailure()
    {
        StubHandler handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway));
        HttpProductClient client = CreateClient(handler);

        PrefsWireDto? prefs = await client.GetPrefsAsync(CancellationToken.None);

        prefs.Should().BeNull();
    }

    [Fact]
    public async Task ConvertAsync_SendsIdempotencyHeader()
    {
        StubHandler handler = new StubHandler(_ => JsonResponse(
            """{"STATUS":"ok","AMOUNT":"1"}"""));
        HttpProductClient client = CreateClient(handler);

        await client.ConvertAsync("BTC", "USD", "1", "idem-123", CancellationToken.None);

        handler.Requests.Should().ContainSingle(r =>
            r.Method == HttpMethod.Post
            && r.Path == "v1/convert"
            && r.IdempotencyKey == "idem-123");
    }

    [Fact]
    public async Task CreateSessionChallengeAndKeyShare_RoundTrip()
    {
        StubHandler handler = new StubHandler(req =>
        {
            if (req.Path == "v1/session/challenge")
            {
                return JsonResponse("""{"CHALLENGE_ID":"ch1","CIPHERTEXT":"abc"}""");
            }

            if (req.Path == "v1/session/key-share")
            {
                return JsonResponse("""{"KEY_SHARE_ID":"ks1","SERVER_PUBLIC_KEY":"pub"}""");
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
        HttpProductClient client = CreateClient(handler);

        SessionChallengeDto challenge = await client.CreateSessionChallengeAsync("wire", CancellationToken.None);
        KeyShareResponseDto share = await client.EstablishKeyShareAsync(
            new KeyShareRequestDto(),
            CancellationToken.None);

        challenge.ChallengeId.Should().Be("ch1");
        share.KeyShareId.Should().Be("ks1");
        handler.Requests.Should().HaveCount(2);
    }

    [Fact]
    public async Task ProductSurface_PostsAndGets_CommonRoutes()
    {
        StubHandler handler = new StubHandler(req =>
        {
            if (req.Method == HttpMethod.Get)
            {
                return req.Path switch
                {
                    var p when p.StartsWith("v1/history", StringComparison.Ordinal) =>
                        JsonResponse("""[]"""),
                    var p when p.StartsWith("v1/quotes", StringComparison.Ordinal) =>
                        JsonResponse("""{"FROM":"BTC","TO":"USD","AMOUNT":"1"}"""),
                    var p when p.StartsWith("v1/receive/", StringComparison.Ordinal) =>
                        JsonResponse("""{"ASSET":"BTC","ADDRESS":"bc1q"}"""),
                    "v1/vault/cards" => JsonResponse("""[]"""),
                    "v1/vault/binaries" => JsonResponse("""[]"""),
                    "v1/account/bootstrap" => JsonResponse("""{}"""),
                    _ => new HttpResponseMessage(HttpStatusCode.NotFound),
                };
            }

            return JsonResponse("""{"STATUS":"ok"}""");
        });
        HttpProductClient client = CreateClient(handler);

        (await client.GetHistoryAsync("BTC", "1d", CancellationToken.None)).Should().BeEmpty();
        (await client.GetQuoteAsync("BTC", "USD", CancellationToken.None)).Should().NotBeNull();
        (await client.GetReceiveAsync("BTC", CancellationToken.None)).Address.Should().Be("bc1q");
        (await client.GetVaultCardsAsync(CancellationToken.None)).Should().BeEmpty();
        (await client.GetVaultBinariesAsync(CancellationToken.None)).Should().BeEmpty();
        await client.PutPrefsAsync(new PrefsWireDto(), CancellationToken.None);
        await client.DeleteVaultCardAsync("card1", CancellationToken.None);
        await client.CreateWalletAsync(new CreateWalletRequestDto { Symbol = "BTC" }, CancellationToken.None);
        await client.TransferAsync("addr", "1", "fast", "idem-t", CancellationToken.None);
        await client.PayAsync("1", new Dictionary<string, string>(), "idem-p", CancellationToken.None);
        await client.AddVaultCardAsync(new VaultCardDto { CardId = "c1" }, "idem-c", CancellationToken.None);
        await client.CreatePosSessionAsync(CancellationToken.None);
        await client.AuthorizePosAsync("s1", CancellationToken.None);
        await client.ConfirmPosAsync("s1", CancellationToken.None);
        (await client.GetAccountBootstrapAsync(CancellationToken.None)).Should().NotBeNull();

        handler.Requests.Should().NotBeEmpty();
    }

    private static HttpProductClient CreateClient(
        StubHandler handler,
        IProductSessionStore? sessions = null)
    {
        HttpClient http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://product.test/"),
        };
        return new HttpProductClient(
            http,
            sessions ?? new InMemoryProductSessionStore(),
            new LabSessionProofBuilder());
    }

    private static HttpResponseMessage JsonResponse(string json)
        => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<RecordedRequest, HttpResponseMessage> _responder;

        public StubHandler(Func<RecordedRequest, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        public List<RecordedRequest> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            string path = request.RequestUri?.IsAbsoluteUri == true
                ? request.RequestUri.PathAndQuery.TrimStart('/')
                : request.RequestUri?.OriginalString ?? string.Empty;
            string? idem = request.Headers.TryGetValues("Idempotency-Key", out IEnumerable<string>? values)
                ? values.FirstOrDefault()
                : null;
            RecordedRequest recorded = new RecordedRequest(request.Method, path, idem);
            Requests.Add(recorded);
            return Task.FromResult(_responder(recorded));
        }
    }

    private sealed record RecordedRequest(HttpMethod Method, string Path, string? IdempotencyKey);
}
