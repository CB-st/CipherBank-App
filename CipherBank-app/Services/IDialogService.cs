// <copyright file="IDialogService.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Services;

/// <summary>
/// Abstraction for displaying dialogs/alerts, enabling testability without Shell dependency.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Displays an alert with a single OK button.
    /// </summary>
    Task ShowAlertAsync(string title, string message, string cancel = "OK");

    /// <summary>
    /// Displays a confirmation dialog with accept and cancel buttons.
    /// Returns true if the user tapped accept, false if cancel.
    /// </summary>
    Task<bool> ShowConfirmAsync(string title, string message, string accept = "OK", string cancel = "Cancel");

    /// <summary>
    /// Prompts for a single line of text. Returns null if cancelled.
    /// </summary>
    Task<string?> PromptAsync(string title, string message, string accept = "OK", string cancel = "Cancel");
}
