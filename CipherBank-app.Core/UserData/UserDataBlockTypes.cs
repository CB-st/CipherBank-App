// <copyright file="UserDataBlockTypes.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>Append-only type registry for userdata pack blocks (v1).</summary>
public static class UserDataBlockTypes
{
    public static string Prefs { get; } = "prefs";

    public static string AppConfig { get; } = "app_config";

    public static string Recipients { get; } = "recipients";

    public static string SessionHints { get; } = "session_hints";

    /// <summary>
    /// Returns true when <paramref name="type"/> is a known v1 registry entry.
    /// Use: Medium (pack validate / restore skip). Scope: UserData pack codec.
    /// </summary>
    public static bool IsKnown(string type)
        => type == Prefs
            || type == AppConfig
            || type == Recipients
            || type == SessionHints;
}
