// <copyright file="ICoraLineProvider.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Cora;

/// <summary>Resolves localizable Cora copy for a screen key.</summary>
public interface ICoraLineProvider
{
    string GetLine(string screen);
}
