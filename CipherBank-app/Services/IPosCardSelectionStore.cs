// <copyright file="IPosCardSelectionStore.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Services;

/// <summary>Stores the non-secret identifier of the card selected for POS.</summary>
public interface IPosCardSelectionStore
{
    string GetSelectedCardId(string fallback);

    void SetSelectedCardId(string cardId);
}
