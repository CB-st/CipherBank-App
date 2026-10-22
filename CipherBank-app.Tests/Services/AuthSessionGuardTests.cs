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
    public async Task CaptureRefreshAsync_LogoutDuringTokenRead_DoesNotRestoreSession()
    {
        using var guard = new AuthSessionGuard();
        var readEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseRead = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
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
        using var guard = new AuthSessionGuard();
        await guard.InvalidateAndClearAsync(() => Task.CompletedTask);

        Func<Task> act = () => guard.CaptureRefreshAsync("old-refresh", () => Task.FromResult<string?>(null));

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task TryPersistAsync_LoginInvalidatesInFlightRefresh()
    {
        using var guard = new AuthSessionGuard();
        var refreshEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseRefresh = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
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
        using var guard = new AuthSessionGuard();
        var revokeEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseRevoke = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
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
        using var guard = new AuthSessionGuard();
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
        using var guard = new AuthSessionGuard();
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
        using var guard = new AuthSessionGuard();
        int generation = await guard.CaptureAsync();
        var writeEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseWrite = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var order = new List<string>();

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
