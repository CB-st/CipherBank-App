// <copyright file="UserDataBlockTypes.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>Append-only type registry for userdata pack blocks (v1).</summary>
public static class UserDataBlockTypes
{
    public const string Prefs = "prefs";

    public const string AppConfig = "app_config";

    public const string Recipients = "recipients";

    public const string SessionHints = "session_hints";

    /// <summary>
    /// Returns true when <paramref name="type"/> is a known v1 registry entry.
    /// Use: Medium (pack validate / restore skip). Scope: UserData pack codec.
    /// </summary>
    public static bool IsKnown(string type)
        => type is Prefs or AppConfig or Recipients or SessionHints;
}
