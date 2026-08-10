// <copyright file="ShellDialogService.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Services;

/// <summary>
/// Shell-based implementation of IDialogService using DisplayAlertAsync.
/// </summary>
public sealed class ShellDialogService : IDialogService
{
    public Task ShowAlertAsync(string title, string message, string cancel = "OK") =>
        Shell.Current.DisplayAlertAsync(title, message, cancel);

    public Task<bool> ShowConfirmAsync(string title, string message, string accept = "OK", string cancel = "Cancel") =>
        Shell.Current.DisplayAlertAsync(title, message, accept, cancel);

    public Task<string?> PromptAsync(string title, string message, string accept = "OK", string cancel = "Cancel") =>
        Shell.Current.DisplayPromptAsync(title, message, accept, cancel);

    /// <inheritdoc />
    public Task<string?> PromptPasswordAsync(string title, string message, string accept = "OK", string cancel = "Cancel") =>
        Shell.Current.DisplayPromptAsync(
            title,
            message,
            accept,
            cancel,
            placeholder: null,
            maxLength: -1,
            keyboard: Keyboard.Numeric,
            initialValue: string.Empty,
            isPassword: true);
}
