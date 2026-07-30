// <copyright file="CoraLines.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Cora;

/// <summary>Dry one-liners per screen.</summary>
public static class CoraLines
{
    private static readonly Dictionary<string, string> Lines = new(StringComparer.OrdinalIgnoreCase)
    {
        ["home"] = "Rates move all day. Your privacy doesn't.",
        ["convert"] = "Locked-in rate. No spread games.",
        ["pay"] = "Rent, paid partly in Dogecoin. Bold. Also completely fine.",
        ["send"] = "Instant. As it should've been all along.",
        ["receive"] = "They see the handle. Not you.",
        ["profile"] = "Your house, your rules. I just keep the keys where you left them.",
        ["keys"] = "No 'forgot password' button here. That's a feature, not an oversight.",
        ["unlock"] = "Welcome back. Prove it's you.",
        ["pos"] = "Token ref only. The PAN never left the vault.",
    };

    public static string For(string screen)
        => Lines.TryGetValue(screen, out var line) ? line : "CipherBank.";
}
