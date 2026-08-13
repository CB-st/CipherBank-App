// <copyright file="ChallengePassZeroTokenOverloadTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.ChallengePass;
using CipherBank_app.ChallengePass.Hybrid;
using CipherBank_app.V1;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Compat;

/// <summary>
/// Guards the zero-token convenience overloads on the ChallengePass ports. The stubs implement only the
/// CancellationToken-required members, matching what app-side HTTP clients provide.
/// </summary>
public class ChallengePassZeroTokenOverloadTests
{
    /// <summary>
    /// Exercises every ChallengePass port zero-token overload and asserts each forwards CancellationToken.None.
    /// Use: Low (regression gate). Scope: this fixture.
    /// </summary>
    [Fact]
    public async Task ChallengePassPorts_ZeroTokenOverloads_ForwardNone()
    {
        RecordingSessionChallengeClient sessions = new RecordingSessionChallengeClient();
        ISessionChallengeClient sessionPort = sessions;
        await sessionPort.RequestChallengeAsync("wire");

        RecordingKeyShareClient keyShare = new RecordingKeyShareClient();
        IPqKeyShareClient keySharePort = keyShare;
        await keySharePort.EstablishAsync(null!);

        RecordingChannelChallengeSource channel = new RecordingChannelChallengeSource();
        IPqChannelChallengeSource channelPort = channel;
        await channelPort.RequestChallengeAsync();

        sessions.Seen.Should().ContainSingle().Which.Should().Be(CancellationToken.None);
        keyShare.Seen.Should().ContainSingle().Which.Should().Be(CancellationToken.None);
        channel.Seen.Should().ContainSingle().Which.Should().Be(CancellationToken.None);
    }

    /// <summary>Records the token every CancellationToken-required member receives.</summary>
    private abstract class TokenRecorder
    {
        public List<CancellationToken> Seen { get; } = [];

        /// <summary>Captures a token and completes with the type default. Use: High (per stubbed call). Scope: one fixture stub.</summary>
        protected Task<T> Record<T>(CancellationToken ct)
        {
            Seen.Add(ct);
            return Task.FromResult<T>(default!);
        }
    }

    /// <summary>ISessionChallengeClient stub implementing only the token-required member.</summary>
    private sealed class RecordingSessionChallengeClient : TokenRecorder, ISessionChallengeClient
    {
        public Task<SessionChallengeDto> RequestChallengeAsync(string accountPublicKeyWire, CancellationToken ct)
            => Record<SessionChallengeDto>(ct);
    }

    /// <summary>IPqKeyShareClient stub implementing only the token-required member.</summary>
    private sealed class RecordingKeyShareClient : TokenRecorder, IPqKeyShareClient
    {
        public Task<PqKeyShareResponse> EstablishAsync(HybridPublicIdentity device, CancellationToken ct)
            => Record<PqKeyShareResponse>(ct);
    }

    /// <summary>IPqChannelChallengeSource stub implementing only the token-required member.</summary>
    private sealed class RecordingChannelChallengeSource : TokenRecorder, IPqChannelChallengeSource
    {
        public Task<SessionChallengeDto> RequestChallengeAsync(CancellationToken ct) => Record<SessionChallengeDto>(ct);
    }
}
