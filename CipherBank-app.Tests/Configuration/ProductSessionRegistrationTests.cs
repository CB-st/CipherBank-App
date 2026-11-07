// <copyright file="ProductSessionRegistrationTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Configuration;
using CipherBank_app.Custody;
using CipherBank_app.Persist;
using CipherBank_app.Services;
using CipherBank_app.Session;
using CipherBank_app.Tests.Persist;
using CipherBank_app.V1;
using CipherBank_app.ViewModels;
using CipherBank_app.Wallets;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
#if DEBUG
using Microsoft.Extensions.Options;
#endif
using Moq;
using Xunit;

namespace CipherBank_app.Tests.Configuration;

/// <summary>Guards proof selection and the production session composition boundary.</summary>
public sealed class ProductSessionRegistrationTests
{
    /// <summary>The parameterless registration must fail closed without a secure proof implementation.</summary>
    [Fact]
    public async Task DefaultRegistration_WithoutSecureProof_RejectsSessionOpenBeforeHttp()
    {
        ServiceCollection services = new();
        services.AddProductSessionServices();
        using ServiceProvider provider = services.BuildServiceProvider();
        await AssertSessionOpenFailsClosedAsync(provider);
    }

    /// <summary>Environment defaults alone never authorize a lab proof, including development defaults.</summary>
    [Theory]
    [InlineData("Production")]
    [InlineData("Development")]
    public async Task CoreDefaults_WithoutExplicitLabOptIn_RejectSessionOpenBeforeHttp(string environment)
    {
        ServiceCollection services = new();
        services.AddCipherBankCore(
            CipherBankDefaultsConfiguration.Build(environment),
            Path.Combine(Path.GetTempPath(), "cb-proof-" + Guid.NewGuid().ToString("N")));
        using ServiceProvider provider = services.BuildServiceProvider();
        await AssertSessionOpenFailsClosedAsync(provider);
    }

    /// <summary>An explicit host choice of lab proof survives the shared default registration.</summary>
    [Fact]
    public async Task ExplicitLabRegistration_IsPreservedAndBuildsLabProof()
    {
        ServiceCollection services = new();
        LabSessionProofBuilder lab = new();
        services.AddSingleton<ISessionProofBuilder>(lab);
        services.AddProductSessionServices();
        using ServiceProvider provider = services.BuildServiceProvider();

        ISessionProofBuilder selected = provider.GetRequiredService<ISessionProofBuilder>();
        selected.Should().BeSameAs(lab);
        object body = await selected.BuildOpenBodyAsync(CancellationToken.None);
        body.Should().BeOfType<Dictionary<string, string>>()
            .Which.Should().Contain("DEVICE_ATTESTATION", LabSessionProofBuilder.LabAttestation);
    }

    /// <summary>The shared feature must not replace a host-supplied secure proof port with lab mode.</summary>
    [Fact]
    public void ExplicitSecureProofRegistration_IsPreserved()
    {
        ServiceCollection services = new();
        ISessionProofBuilder secureProof = Mock.Of<ISessionProofBuilder>();
        services.AddSingleton(secureProof);
        services.AddProductSessionServices();
        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ISessionProofBuilder>().Should().BeSameAs(secureProof);
    }

    /// <summary>Only external ports are supplied; the feature must compose the real lifecycle services.</summary>
    [Fact]
    public async Task Registration_ResolvesProductionLifecycle_AndDeniedCustodyCannotOpenSession()
    {
        ServiceCollection services = new();
        Mock<IProductClient> client = new(MockBehavior.Strict);
        Mock<ICustodyService> custody = new();
        custody.Setup(c => c.UnlockAsync("000000")).ReturnsAsync(false);
        services.AddSingleton(client.Object);
        services.AddSingleton(custody.Object);
        services.AddSingleton(Mock.Of<ILocalWalletSeeder>());
        services.AddSingleton(Mock.Of<IPrefsStore>());
        services.AddSingleton(Mock.Of<IRecipientRepository>());
        services.AddSingleton(Mock.Of<IStreamService>());
        services.AddSingleton(TimeProvider.System);
        services.AddProductSessionServices();
        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });

        provider.GetRequiredService<IProductSessionCoordinator>().Should().BeOfType<ProductSessionCoordinator>();
        provider.GetRequiredService<IStreamHub>().Should().BeOfType<StreamHub>();
        provider.GetRequiredService<IPrefsSyncService>().Should().BeOfType<PrefsSyncService>();
        provider.GetRequiredService<IAccountBootstrapService>().Should().BeOfType<AccountBootstrapService>();
        IAppSession session = provider.GetRequiredService<IAppSession>();
        session.Should().BeOfType<AppSession>();
        provider.GetRequiredService<IAppSession>().Should().BeSameAs(session);

        (await session.UnlockAsync("000000")).Should().BeFalse();
        session.AccessToken.Should().BeNull();
        client.VerifyNoOtherCalls();
    }

    /// <summary>Production rejects lab opt-in before mutating the collection.</summary>
    [Fact]
    public void Production_RejectsExplicitLabProof()
    {
        ServiceCollection services = new();
        Action register = () => services.AddProductSessionServices(useLabProof: true, isDevelopment: false);
        register.Should().Throw<InvalidOperationException>();
        services.Should().BeEmpty();
    }

    /// <summary>Development still requires a deliberate opt-in, even after Core registered defaults.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Development_SelectsLabOnlyWithExplicitOptIn(bool optIn)
    {
        ServiceCollection services = new();
        services.AddProductSessionServices();
        services.AddProductSessionServices(useLabProof: optIn, isDevelopment: true);
        using ServiceProvider provider = services.BuildServiceProvider();
        if (optIn)
        {
            object body = await provider.GetRequiredService<ISessionProofBuilder>()
                .BuildOpenBodyAsync(CancellationToken.None);
            body.Should().BeOfType<Dictionary<string, string>>()
                .Which.Should().Contain("DEVICE_ATTESTATION", LabSessionProofBuilder.LabAttestation);
        }
        else
        {
            await AssertSessionOpenFailsClosedAsync(provider);
        }
    }

    /// <summary>Repeated feature registration retains one stateful service and host overrides.</summary>
    [Fact]
    public void RepeatedRegistration_PreservesHostPortsAndSingletonDescriptors()
    {
        ServiceCollection services = new();
        ISessionProofBuilder proof = Mock.Of<ISessionProofBuilder>();
        IProductSessionStore store = Mock.Of<IProductSessionStore>();
        services.AddSingleton(proof);
        services.AddSingleton(store);
        services.AddProductSessionServices();
        services.AddProductSessionServices(useLabProof: true, isDevelopment: true);
        services.Count(descriptor => descriptor.ServiceType == typeof(IAppSession)).Should().Be(1);
        services.Count(descriptor => descriptor.ServiceType == typeof(IProductSessionCoordinator)).Should().Be(1);
        using ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<ISessionProofBuilder>().Should().BeSameAs(proof);
        provider.GetRequiredService<IProductSessionStore>().Should().BeSameAs(store);
    }

    /// <summary>The host stream path resolves eagerly but rolls back a later unlock without an endpoint.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a uri")]
    [InlineData("ws://stream.invalid/")]
    [InlineData("https://stream.invalid/")]
    public async Task UnavailableHostStream_ResolvesSessionAndFailsUnlockClosed(string? endpoint)
    {
        ServiceCollection services = new();
        Mock<ICustodyService> custody = new();
        custody.Setup(c => c.UnlockAsync("123456")).ReturnsAsync(true);
        Mock<IProductClient> client = new(MockBehavior.Strict);
        client.Setup(c => c.CreateSessionAsync(CancellationToken.None))
            .ReturnsAsync(new SessionDto { AccessToken = "access", RefreshToken = "refresh" });
        Mock<IPrefsStore> prefs = new();
        prefs.Setup(p => p.LoadAsync(CancellationToken.None)).ReturnsAsync(new UserPrefs(TestPreferenceDefaults.Value));
        services.AddSingleton(custody.Object);
        services.AddSingleton(client.Object);
        services.AddSingleton(prefs.Object);
        services.AddSingleton(Mock.Of<ILocalWalletSeeder>());
        services.AddSingleton(Mock.Of<IRecipientRepository>());
        services.AddSingleton(TimeProvider.System);
        services.AddProductSessionServices();
        services.AddProductSessionStream(endpoint);
        services.AddSingleton<Microsoft.Extensions.Logging.ILogger<LoginViewModel>>(
            NullLogger<LoginViewModel>.Instance);
        services.AddSingleton(Mock.Of<INavigationService>());
        services.AddSingleton(Mock.Of<IDialogService>());
#if DEBUG
        Mock<ISettingsService> settings = new();
        settings.SetupGet(s => s.Environment).Returns("Production");
        services.AddSingleton(settings.Object);
        services.AddSingleton(Options.Create(new HostBehaviorOptions()));
#endif
        services.AddTransient<LoginViewModel>();
        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });

        using LoginViewModel login = provider.GetRequiredService<LoginViewModel>();
        IAppSession session = provider.GetRequiredService<IAppSession>();
        provider.GetRequiredService<IStreamService>().Should().BeOfType<UnavailableStreamService>();
        (await session.UnlockAsync("123456")).Should().BeFalse();
        session.AccessToken.Should().BeNull();
        (await provider.GetRequiredService<IProductSessionStore>().GetAsync()).Should().BeNull();
        custody.Verify(c => c.Lock(), Times.Once);
        client.Verify(c => c.CreateSessionAsync(CancellationToken.None), Times.Once);
        client.VerifyNoOtherCalls();
    }

    /// <summary>A configured secure endpoint selects the live adapter without connecting at resolution.</summary>
    [Fact]
    public async Task HostStream_UsesExplicitSecureEndpointAndPreservesOverrides()
    {
        ServiceCollection services = new();
        services.AddProductSessionStream("wss://stream.invalid/");
        await using ServiceProvider provider = services.BuildServiceProvider();
        IStreamService stream = provider.GetRequiredService<IStreamService>();
        stream.Should().BeOfType<ClientWebSocketStreamService>();
        stream.IsConnected.Should().BeFalse();

        ServiceCollection overridden = new();
        IStreamService supplied = Mock.Of<IStreamService>();
        overridden.AddSingleton(supplied);
        overridden.AddProductSessionStream(null);
        using ServiceProvider overriddenProvider = overridden.BuildServiceProvider();
        overriddenProvider.GetRequiredService<IStreamService>().Should().BeSameAs(supplied);
    }

    private static async Task AssertSessionOpenFailsClosedAsync(ServiceProvider provider)
    {
        using RecordingSessionHandler handler = new();
        using HttpClient http = new(handler) { BaseAddress = new Uri("https://session.invalid/") };
        IProductSessionStore store = provider.GetRequiredService<IProductSessionStore>();
        HttpProductClient client = new(http, store, provider.GetRequiredService<ISessionProofBuilder>());

        Func<Task> open = async () => await client.CreateSessionAsync(CancellationToken.None);
        await open.Should().ThrowAsync<InvalidOperationException>();
        handler.RequestCount.Should().Be(0, "no session request is authorized without a secure proof");
        (await store.GetAsync()).Should().BeNull();
    }

    private sealed class RecordingSessionHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"ACCESS_TOKEN\":\"test-access\",\"REFRESH_TOKEN\":\"test-refresh\"}"),
            });
        }
    }
}
