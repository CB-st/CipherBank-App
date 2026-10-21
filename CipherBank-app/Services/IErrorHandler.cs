// <copyright file="IErrorHandler.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Services;

/// <summary>
/// Centralizes API error handling for ViewModels: HttpRequestException,
/// UnauthorizedAccessException, OperationCanceledException, and a 401 nested at any depth.
/// </summary>
public interface IErrorHandler
{
    /// <summary>
    /// Executes the operation and handles common API errors.
    /// On a 401 at any exception depth, clears the stored session and token, then navigates to Login unless that redirect would loop.
    /// </summary>
    /// <param name="operation">The async operation to execute.</param>
    /// <param name="setErrorMessage">Action to set the error message (e.g. ErrorMessage = value).</param>
    /// <param name="networkErrorMessage">Optional custom message for a non-401 HttpRequestException. Default: "Network error. Please check your connection."</param>
    /// <returns>True if operation succeeded, false if an error was caught and handled.</returns>
    Task<bool> HandleApiErrorsAsync(
        Func<Task> operation,
        Action<string?> setErrorMessage,
        string? networkErrorMessage = null);
}
