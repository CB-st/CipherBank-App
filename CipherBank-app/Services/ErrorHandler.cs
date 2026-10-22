// <copyright file="ErrorHandler.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Net;
using CipherBank_app.Constants;

namespace CipherBank_app.Services;

/// <summary>
/// Centralizes API error handling: sets ErrorMessage, clears the stored session, and navigates to Login on 401.
/// </summary>
public sealed class ErrorHandler : IErrorHandler
{
    private readonly INavigationService _navigation;
    private readonly IAuthService _auth;
    private int _loginRedirectInProgress;

    public ErrorHandler(INavigationService navigation, IAuthService auth)
    {
        ArgumentNullException.ThrowIfNull(navigation);
        ArgumentNullException.ThrowIfNull(auth);
        _navigation = navigation;
        _auth = auth;
    }

    public async Task<bool> HandleApiErrorsAsync(
        Func<Task> operation,
        Action<string?> setErrorMessage,
        string? networkErrorMessage = null)
    {
        int generation = await _auth.CaptureSessionAsync();
        try
        {
            await operation();
            return true;
        }
        catch (Exception ex) when (SessionExpiry.IsExpired(ex))
        {
            setErrorMessage("Session expired. Please log in again.");
            await InvalidateSessionAndRedirectToLoginAsync(generation);
            return false;
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
    }

    /// <inheritdoc />
    public bool IsSessionExpired(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return SessionExpiry.IsExpired(exception);
    }

    private async Task InvalidateSessionAndRedirectToLoginAsync(int generation)
    {
        if (!TryBeginLoginRedirect())
        {
            return;
        }

        try
        {
            if (!await _auth.LogoutAsync(generation))
            {
                return;
            }

            if (!SessionExpiry.IsLoginRoute(_navigation.CurrentRoute))
            {
                await _navigation.GoToAsync(Routes.Login);
            }
        }
        finally
        {
            EndLoginRedirect();
        }
    }

    private bool TryBeginLoginRedirect()
        => Interlocked.CompareExchange(ref _loginRedirectInProgress, 1, 0) == 0;

    private void EndLoginRedirect()
        => Interlocked.Exchange(ref _loginRedirectInProgress, 0);

    private static class SessionExpiry
    {
        public static bool IsExpired(Exception exception)
        {
            var pending = new Stack<Exception>();
            var seen = new HashSet<Exception>(ReferenceEqualityComparer.Instance);
            pending.Push(exception);

            while (pending.TryPop(out Exception? current))
            {
                if (!seen.Add(current))
                {
                    continue;
                }

                if (IsUnauthorized(current))
                {
                    return true;
                }

                PushNested(pending, current);
            }

            return false;
        }

        public static bool IsLoginRoute(string? currentRoute)
        {
            if (string.IsNullOrWhiteSpace(currentRoute))
            {
                return false;
            }

            return string.Equals(
                RouteSegment(currentRoute),
                RouteSegment(Routes.Login),
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsUnauthorized(Exception current)
            => current is UnauthorizedAccessException
                || (current is HttpRequestException http && http.StatusCode == HttpStatusCode.Unauthorized);

        private static void PushNested(Stack<Exception> pending, Exception current)
        {
            if (current is AggregateException aggregate)
            {
                for (int index = aggregate.InnerExceptions.Count - 1; index >= 0; index--)
                {
                    pending.Push(aggregate.InnerExceptions[index]);
                }

                return;
            }

            if (current.InnerException is not null)
            {
                pending.Push(current.InnerException);
            }
        }

        private static string RouteSegment(string route)
        {
            string path = StripQueryAndFragment(route.Trim()).TrimEnd('/');
            int slash = path.LastIndexOf('/');
            return slash >= 0 ? path[(slash + 1)..] : path;
        }

        private static string StripQueryAndFragment(string route)
        {
            int cutoff = FirstSeparator(route.IndexOf('?'), route.IndexOf('#'));
            return cutoff >= 0 ? route[..cutoff] : route;
        }

        private static int FirstSeparator(int query, int fragment)
        {
            if (query < 0)
            {
                return fragment;
            }

            if (fragment < 0)
            {
                return query;
            }

            return Math.Min(query, fragment);
        }
    }
}
