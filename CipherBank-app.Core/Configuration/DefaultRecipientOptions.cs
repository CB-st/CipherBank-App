// <copyright file="DefaultRecipientOptions.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Configuration;

/// <summary>
/// Demo payee seed row. Routing and account are input-only mask sources, not stored secrets.
/// </summary>
public sealed class DefaultRecipientOptions
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Holder { get; set; }

    public string? Bank { get; set; }

    public string? Routing { get; set; }

    public string? Account { get; set; }

    public string AccountType { get; set; } = "checking";

    public string? Memo { get; set; }
}
