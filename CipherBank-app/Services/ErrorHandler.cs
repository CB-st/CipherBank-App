// <copyright file="ErrorHandler.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Constants;

namespace CipherBank_app.Services;

/// <summary>
/// Centralizes API error handling: sets ErrorMessage, navigates to Login on 401.
/// </summary>
public sealed class ErrorHandler(INavigationService navigation) : IErrorHandler
{
    public async Task<bool> HandleApiErrorsAsync(
        Func<Task> operation,
        Action<string?> setErrorMessage,
        string? networkErrorMessage = null)
    {
        try
        {
            await operation();
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (HttpRequestException)
        {
            setErrorMessage(networkErrorMessage ?? "Network error. Please check your connection.");
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            setErrorMessage("Session expired. Please log in again.");
            await navigation.GoToAsync(Routes.Login);
            return false;
        }
    }
}
