// <copyright file="IErrorHandler.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Services;

/// <summary>
/// Centralizes API error handling for ViewModels: HttpRequestException,
/// UnauthorizedAccessException, OperationCanceledException.
/// </summary>
public interface IErrorHandler
{
    /// <summary>
    /// Executes the operation and handles common API errors.
    /// Sets error message and navigates to Login on 401.
    /// </summary>
    /// <param name="operation">The async operation to execute.</param>
    /// <param name="setErrorMessage">Action to set the error message (e.g. ErrorMessage = value).</param>
    /// <param name="networkErrorMessage">Optional custom message for HttpRequestException. Default: "Network error. Please check your connection."</param>
    /// <returns>True if operation succeeded, false if an error was caught and handled.</returns>
    Task<bool> HandleApiErrorsAsync(
        Func<Task> operation,
        Action<string?> setErrorMessage,
        string? networkErrorMessage = null);
}
