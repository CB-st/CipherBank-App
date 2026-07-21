// <copyright file="HoldingVisibilityTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Persist;
using CipherBank_app.V1;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Persist;

public class HoldingVisibilityTests
{
    [Fact]
    public void Split_PlacesEnabledSymbolsCaseInsensitivelyInVisibleHoldings()
    {
        var holdings = new[]
        {
            new HoldingDto { Symbol = "btc" },
            new HoldingDto { Symbol = "ETH" },
            new HoldingDto { Symbol = "usd" },
        };

        HoldingVisibilityResult result = HoldingVisibility.Split(holdings, new List<string> { "BTC", "Usd" });

        result.Visible.Select(holding => holding.Symbol).Should().Equal("btc", "usd");
        result.Other.Select(holding => holding.Symbol).Should().Equal("ETH");
    }

    [Fact]
    public void Split_UsesDefaultEnabledCurrenciesWhenNoneAreConfigured()
    {
        var holdings = new[]
        {
            new HoldingDto { Symbol = "BTC" },
            new HoldingDto { Symbol = "XMR" },
            new HoldingDto { Symbol = "USD" },
            new HoldingDto { Symbol = "ETH" },
        };

        HoldingVisibilityResult result = HoldingVisibility.Split(holdings, Array.Empty<string>());

        result.Visible.Select(holding => holding.Symbol).Should().Equal("BTC", "XMR", "USD");
        result.Other.Select(holding => holding.Symbol).Should().Equal("ETH");
    }
}
