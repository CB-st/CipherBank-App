// <copyright file="ErrorHandler.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Net.Http;
using CipherBank_app.Constants;

namespace CipherBank_app.Services;

/// <summary>
/// Centralizes API error handling: sets ErrorMessage, navigates to Login on 401.
/// </summary>
public sealed class ErrorHandler : IErrorHandler
{
    private readonly INavigationService _navigation;

    public ErrorHandler(INavigationService navigation)
    {
        _navigation = navigation;
    }

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
            await _navigation.GoToAsync(Routes.Login);
            return false;
        }
    }
}
