// <copyright file="H1H2TestRig.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Net;
using System.Net.Http.Json;
using CipherBank_app.Configuration;
using CipherBank_app.Custody;
using CipherBank_app.Persist;
using CipherBank_app.Services.Handlers;
using CipherBank_app.Session;
using CipherBank_app.Tests.Persist;
using CipherBank_app.V1;
using CipherBank_app.Wallets;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CipherBank_app.Tests.Session.Regression;

/// <summary>Boundary fixtures only; lifecycle decisions belong to the real product objects.</summary>
internal sealed class H1H2TestRig : IDisposable
{
    internal static readonly DateTimeOffset Now = new(2035, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private readonly List<HttpClient> _httpClients = [];
    private bool _unlocked;

    internal H1H2TestRig(
        IProductSessionStore? store = null,
        IProductClient? client = null,
        Func<IServiceProvider, IProductClient>? clientFactory = null)
    {
        Store = store ?? new InMemoryProductSessionStore();
        Custody.SetupGet(c => c.IsUnlocked).Returns(() => _unlocked);
        Custody.Setup(c => c.UnlockAsync(It.IsAny<string>())).Returns(() => UnlockCustodyAsync());
        Custody.Setup(c => c.UnlockWithDeviceSecretAsync()).Returns(() => UnlockCustodyAsync());
        Custody.Setup(c => c.SealAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(async () =>
            {
                await UnlockCustodyAsync();
                Sealed = true;
            });
        Custody.Setup(c => c.HasSealedWalletAsync()).ReturnsAsync(() => Sealed);
        Custody.Setup(c => c.Lock()).Callback(Expire);
        Prefs.Setup(p => p.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new UserPrefs(TestPreferenceDefaults.Value) { LockIdleSeconds = 120 });
        Prefs.Setup(p => p.LoadAsync())
            .ReturnsAsync(() => new UserPrefs(TestPreferenceDefaults.Value) { LockIdleSeconds = 120 });
        Sync.Setup(p => p.PullMergeAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        Bootstrap.Setup(b => b.ApplyAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        Product.Setup(p => p.CreateSessionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Credential("current"));

        ServiceCollection services = new();
        services.AddProductSessionServices();
        // Exact relevant M7 ordering: Core default, THEN a host-owned singleton interface override.
        services.AddSingleton(Store);
        services.AddSingleton<TimeProvider>(new FixedClock());
        services.AddSingleton(Custody.Object);
        services.AddSingleton(Prefs.Object);
        services.AddSingleton(Sync.Object);
        services.AddSingleton(Bootstrap.Object);
        services.AddSingleton<IStreamService>(Stream);
        if (clientFactory is null)
        {
            services.AddSingleton(client ?? Product.Object);
        }
        else
        {
            services.AddSingleton(clientFactory);
        }

        services.AddSingleton(Mock.Of<ILocalWalletSeeder>());
        services.AddSingleton<ISessionProofBuilder>(new LabSessionProofBuilder());
        Services = services.BuildServiceProvider();
        Coordinator = Services.GetRequiredService<IProductSessionCoordinator>();
        Hub = Services.GetRequiredService<IStreamHub>();
        App = Services.GetRequiredService<IAppSession>();
    }

    internal ServiceProvider Services { get; }

    internal IProductSessionStore Store { get; }

    internal Mock<ICustodyService> Custody { get; } = new();

    internal Mock<IPrefsStore> Prefs { get; } = new();

    internal Mock<IPrefsSyncService> Sync { get; } = new();

    internal Mock<IAccountBootstrapService> Bootstrap { get; } = new();

    internal Mock<IProductClient> Product { get; } = new();

    internal StreamProbe Stream { get; } = new();

    internal IProductSessionCoordinator Coordinator { get; }

    internal IStreamHub Hub { get; }

    internal IAppSession App { get; }

    internal AsyncGate? CustodyGate { get; set; }

    internal bool Sealed { get; private set; }

    public void Dispose()
    {
        foreach (HttpClient http in _httpClients)
        {
            http.Dispose();
        }

        Services.Dispose();
    }

    internal static SessionDto Credential(string name) => new()
    {
        AccessToken = name + "-access",
        RefreshToken = name + "-refresh",
        ExpiresAt = Now.AddHours(1).ToUnixTimeMilliseconds(),
    };

    internal static HttpResponseMessage Json(object body) => new(HttpStatusCode.OK) { Content = JsonContent.Create(body) };

    internal static async Task<Exception?> ObserveAsync(Func<Task> action)
    {
        Exception? error = await Record.ExceptionAsync(async () => await action().WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.True(
            error is null or OperationCanceledException or InvalidOperationException or HttpRequestException or UnauthorizedAccessException,
            $"Unexpected failure: {error}");
        return error;
    }

    internal HttpProductClient Http(Transport transport, bool shell = false)
    {
        DelegatingHandler bearer = shell
            ? new AuthHeaderHandler(Services, new FixedClock())
            : Services.GetRequiredService<ProductAuthHeaderHandler>();
        bearer.InnerHandler = transport;
        HttpClient http = new(bearer) { BaseAddress = new Uri("https://product.test/") };
        _httpClients.Add(http);
        return ActivatorUtilities.CreateInstance<HttpProductClient>(Services, http);
    }

    internal void Expire()
    {
        _unlocked = false;
        Custody.Raise(c => c.Locked += null, EventArgs.Empty);
    }

    private async Task<bool> UnlockCustodyAsync()
    {
        if (CustodyGate is { } gate)
        {
            await gate.PauseAsync(CancellationToken.None);
        }

        _unlocked = true;
        return true;
    }

    internal sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    internal sealed class AsyncGate
    {
        private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        internal Task Entered => _entered.Task.WaitAsync(TimeSpan.FromSeconds(10));

        internal async Task PauseAsync(CancellationToken ct = default)
        {
            _entered.TrySetResult();
            await _release.Task.WaitAsync(ct);
        }

        internal async Task WaitForEntryAsync(Task operation)
        {
            try
            {
                Task entered = Entered;
                Task first = await Task.WhenAny(entered, operation);
                Assert.True(first == entered, "Operation completed before the configured fixture boundary was entered.");
                await entered;
            }
            catch
            {
                Release();
                throw;
            }
        }

        internal void Release() => _release.TrySetResult();
    }

    internal sealed class Transport(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        internal Task<HttpResponseMessage> InvokeAsync(HttpRequestMessage request, CancellationToken ct) => send(request, ct);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => send(request, cancellationToken);
    }

    internal sealed class StreamProbe : IStreamService
    {
        public event EventHandler<StreamEventArgs>? EventReceived;

        public bool IsConnected { get; private set; }

        internal AsyncGate? ConnectGate { get; set; }

        public async Task ConnectAsync(CancellationToken ct)
        {
            if (ConnectGate is { } gate)
            {
                await gate.PauseAsync(CancellationToken.None); // Intentionally adversarial: handshake ignores caller cancellation.
            }

            IsConnected = true;
            Emit();
        }

        public Task DisconnectAsync()
        {
            IsConnected = false;
            return Task.CompletedTask;
        }

        internal void Emit() => EventReceived?.Invoke(this, new StreamEventArgs { Type = "fixture" });
    }
}
