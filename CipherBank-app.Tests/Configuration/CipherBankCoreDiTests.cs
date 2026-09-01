// <copyright file="CipherBankCoreDiTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Configuration;
using CipherBank_app.Cora;
using CipherBank_app.Custody;
using CipherBank_app.Persist;
using CipherBank_app.Pos;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace CipherBank_app.Tests.Configuration;

public class CipherBankCoreDiTests
{
    /// <summary>
    /// Resolves the platform-neutral Core registrations from defaults configuration.
    /// Use: Medium (DI smoke / new_coverage). Scope: CipherBankCoreDiTests.
    /// </summary>
    [Fact]
    public void AddCipherBankCore_ResolvesPersistCryptoAndCopyServices()
    {
        string databaseDirectory = Path.Combine(Path.GetTempPath(), "cb-di-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(databaseDirectory);

        ServiceCollection services = new ServiceCollection();
        services.AddCipherBankCore(CipherBankDefaultsConfiguration.Build(), databaseDirectory);
        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ICryptoBox>().Should().BeOfType<AesGcmCryptoBox>();
        provider.GetRequiredService<ICoraLineProvider>().Should().BeOfType<CoraLineProvider>();
        provider.GetRequiredService<IEmvExchangeSimulator>().Should().BeOfType<EmvExchangeSimulator>();
        LocalDb localDb = provider.GetRequiredService<ILocalDb>().Should().BeOfType<LocalDb>().Subject;
        PersistenceOptions persistence = provider.GetRequiredService<IOptions<PersistenceOptions>>().Value;
        Path.GetFileName(localDb.Path).Should().Be(persistence.DatabaseName);
        persistence.DefaultRecipients.Select(row => row.Id).Should().Equal(
            "seed:rent-4th-st",
            "seed:utilities-co");
        provider.GetRequiredService<IWalletRepository>().Should().BeOfType<WalletRepository>();
        provider.GetRequiredService<IRecipientRepository>().Should().BeOfType<RecipientRepository>();
        provider.GetRequiredService<IMarketRepository>().Should().BeOfType<MarketRepository>();
        provider.GetRequiredService<IPrefsStore>().Should().BeOfType<PrefsStore>();
        provider.GetRequiredService<IRatesCache>().Should().BeOfType<RatesCache>();
        provider.GetRequiredService<ISyncJobScheduler>().Should().BeOfType<SyncJobScheduler>();
    }

    /// <summary>
    /// Rejects null/blank host arguments before any service is registered.
    /// Use: Low. Scope: CipherBankCoreDiTests argument guards.
    /// </summary>
    [Fact]
    public void AddCipherBankCore_RejectsInvalidArguments()
    {
        ServiceCollection services = new ServiceCollection();
        Action nullConfig = () => services.AddCipherBankCore(null!, Path.GetTempPath());
        Action blankDir = () => services.AddCipherBankCore(CipherBankDefaultsConfiguration.Build(), " ");

        nullConfig.Should().Throw<ArgumentNullException>();
        blankDir.Should().Throw<ArgumentException>();
    }
}
