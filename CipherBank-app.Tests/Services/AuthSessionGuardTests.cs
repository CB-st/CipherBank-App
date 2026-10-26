// <copyright file="AuthSessionGuardTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Services;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Services;

public class AuthSessionGuardTests
{
    [Fact]
    public void CaptureRefreshAsync_NullDelegate_ThrowsSynchronously()
    {
        using AuthSessionGuard guard = new();
        Func<Task<string?>>? readRefreshToken = null;
        Action act = () => guard.CaptureRefreshAsync("refresh", readRefreshToken!);

        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public async Task CaptureLogoutAsync_LoginDuringTokenRead_CapturesOnlyOwnedToken()
    {
        using AuthSessionGuard guard = new();
        string stored = "session-a";
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int generation = await guard.CaptureAsync();
        Task<(int Generation, string? RefreshToken)?> capture = guard.CaptureLogoutAsync(
            async () =>
            {
                entered.SetResult();
                await release.Task;
                return stored;
            },
            generation);
        await entered.Task;
        Task<bool> login = guard.TryPersistAsync(
            generation,
            () =>
            {
                stored = "session-b";
                return Task.CompletedTask;
            },
            startsSession: true);
        login.IsCompleted.Should().BeFalse();
        release.SetResult();
        (int Generation, string? RefreshToken)? session = await capture;
        (await login).Should().BeTrue();

        session.Should().NotBeNull();
        session!.Value.RefreshToken.Should().Be("session-a");
        bool cleared = await guard.InvalidateAndClearAsync(
            () =>
            {
                stored = string.Empty;
                return Task.CompletedTask;
            },
            capturedGeneration: session.Value.Generation);
        cleared.Should().BeFalse();
        stored.Should().Be("session-b");
        (await guard.CaptureLogoutAsync(() => Task.FromResult<string?>(stored), generation)).Should().BeNull();
    }

    [Fact]
    public async Task CaptureRefreshAsync_LogoutDuringTokenRead_DoesNotRestoreSession()
    {
        using AuthSessionGuard guard = new();
        TaskCompletionSource readEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<int> capture = guard.CaptureRefreshAsync("old-refresh", async () =>
        {
            readEntered.SetResult();
            await releaseRead.Task;
            return "old-refresh";
        });
        await readEntered.Task;
        await guard.InvalidateAndClearAsync(() => Task.CompletedTask);
        releaseRead.SetResult();

        bool wrote = false;
        bool stored = await guard.TryPersistAsync(await capture, () =>
        {
            wrote = true;
            return Task.CompletedTask;
        });

        stored.Should().BeFalse();
        wrote.Should().BeFalse();
    }

    [Fact]
    public async Task CaptureRefreshAsync_OldTokenPresentedAfterLogout_RejectsToken()
    {
        using AuthSessionGuard guard = new();
        await guard.InvalidateAndClearAsync(() => Task.CompletedTask);

        Func<Task> act = () => guard.CaptureRefreshAsync("old-refresh", () => Task.FromResult<string?>(null));

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task TryPersistAsync_LoginInvalidatesInFlightRefresh()
    {
        using AuthSessionGuard guard = new();
        TaskCompletionSource refreshEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseRefresh = new(TaskCreationOptions.RunContinuationsAsynchronously);
        string storedToken = "old";
        async Task<bool> RefreshAsync()
        {
            int generation = await guard.CaptureAsync();
            refreshEntered.SetResult();
            await releaseRefresh.Task;
            return await guard.TryPersistAsync(generation, () =>
            {
                storedToken = "refreshed-old";
                return Task.CompletedTask;
            });
        }

        Task<bool> refresh = RefreshAsync();
        await refreshEntered.Task;
        int loginGeneration = await guard.CaptureAsync();
        await guard.TryPersistAsync(
            loginGeneration,
            () =>
            {
                storedToken = "new-account";
                return Task.CompletedTask;
            },
            startsSession: true);
        releaseRefresh.SetResult();

        (await refresh).Should().BeFalse();
        storedToken.Should().Be("new-account");
    }

    [Fact]
    public async Task InvalidateAndClearAsync_SlowLogoutCannotClearNewLogin()
    {
        using AuthSessionGuard guard = new();
        TaskCompletionSource revokeEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseRevoke = new(TaskCreationOptions.RunContinuationsAsynchronously);
        bool cleared = false;
        async Task LogoutAsync()
        {
            int generation = await guard.CaptureAsync();
            revokeEntered.SetResult();
            await releaseRevoke.Task;
            await guard.InvalidateAndClearAsync(
                () =>
                {
                    cleared = true;
                    return Task.CompletedTask;
                },
                capturedGeneration: generation);
        }

        Task logout = LogoutAsync();
        await revokeEntered.Task;
        int loginGeneration = await guard.CaptureAsync();
        await guard.TryPersistAsync(loginGeneration, () => Task.CompletedTask, startsSession: true);
        releaseRevoke.SetResult();
        await logout;

        cleared.Should().BeFalse();
    }

    [Fact]
    public async Task TryPersistAsync_GenerationCapturedBeforeLogout_DoesNotWrite()
    {
        using AuthSessionGuard guard = new();
        int generation = await guard.CaptureAsync();
        bool cleared = false;
        await guard.InvalidateAndClearAsync(() =>
        {
            cleared = true;
            return Task.CompletedTask;
        });

        bool wrote = false;
        bool stored = await guard.TryPersistAsync(generation, () =>
        {
            wrote = true;
            return Task.CompletedTask;
        });

        cleared.Should().BeTrue();
        stored.Should().BeFalse();
        wrote.Should().BeFalse();
    }

    [Fact]
    public async Task TryPersistAsync_GenerationCapturedAfterLogout_Writes()
    {
        using AuthSessionGuard guard = new();
        await guard.InvalidateAndClearAsync(() => Task.CompletedTask);
        int generation = await guard.CaptureAsync();
        bool wrote = false;

        bool stored = await guard.TryPersistAsync(generation, () =>
        {
            wrote = true;
            return Task.CompletedTask;
        });

        stored.Should().BeTrue();
        wrote.Should().BeTrue();
    }

    [Fact]
    public async Task TryPersistAsync_WriteStartedBeforeLogout_FinishesBeforeClearAndBlocksStaleReplay()
    {
        using AuthSessionGuard guard = new();
        int generation = await guard.CaptureAsync();
        TaskCompletionSource writeEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseWrite = new(TaskCreationOptions.RunContinuationsAsynchronously);
        List<string> order = new();

        Task<bool> persist = guard.TryPersistAsync(generation, async () =>
        {
            order.Add("write");
            writeEntered.SetResult();
            await releaseWrite.Task;
        });

        await writeEntered.Task;
        Task clear = guard.InvalidateAndClearAsync(() =>
        {
            order.Add("clear");
            return Task.CompletedTask;
        });

        releaseWrite.SetResult();
        (await persist).Should().BeTrue();
        await clear;

        bool replayed = await guard.TryPersistAsync(generation, () =>
        {
            order.Add("replay");
            return Task.CompletedTask;
        });

        replayed.Should().BeFalse();
        order.Should().Equal("write", "clear");
    }
}
