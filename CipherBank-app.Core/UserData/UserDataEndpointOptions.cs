// <copyright file="UserDataEndpointOptions.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>
/// Flexible TCP endpoint for userdata (production host or loopback self-server).
/// Default production: internal.cipherbank.money:53809, EOF CRLFCRLF.
/// </summary>
public sealed class UserDataEndpointOptions
{
    public const string DefaultProductionHost = "internal.cipherbank.money";
    public const int DefaultPort = 53809;
    public const string DefaultEof = "\r\n\r\n";

    public string Host { get; init; } = DefaultProductionHost;

    public int Port { get; init; } = DefaultPort;

    public string EndOfFrame { get; init; } = DefaultEof;

    public TimeSpan ConnectTimeout { get; init; } = TimeSpan.FromSeconds(10);

    public TimeSpan IoTimeout { get; init; } = TimeSpan.FromSeconds(30);

    public UserDataPayloadMode PayloadMode { get; init; } = UserDataPayloadMode.PlainJson;

    /// <summary>
    /// Production routing target (still needs MasterKeyEncrypted codec + keys to speak live src).
    /// Use: Low (Shell DI). Scope: userdata transport.
    /// </summary>
    public static UserDataEndpointOptions Production()
        => new()
        {
            Host = DefaultProductionHost,
            Port = DefaultPort,
            PayloadMode = UserDataPayloadMode.MasterKeyEncrypted,
        };

    /// <summary>
    /// Localhost self-test / E2E harness target. Use: High (tests). Scope: userdata transport.
    /// </summary>
    public static UserDataEndpointOptions Loopback(int port, UserDataPayloadMode mode = UserDataPayloadMode.PlainJson)
        => new()
        {
            Host = "127.0.0.1",
            Port = port,
            PayloadMode = mode,
        };
}
