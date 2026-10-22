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
