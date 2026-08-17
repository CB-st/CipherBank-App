// <copyright file="IThemeColorProvider.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Services;

/// <summary>Resolves a semantic MAUI color resource for ViewModel-owned presentation data.</summary>
public interface IThemeColorProvider
{
    Color GetColor(string resourceKey);
}
