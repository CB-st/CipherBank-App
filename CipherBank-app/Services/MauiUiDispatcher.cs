// <copyright file="MauiUiDispatcher.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Services;

/// <inheritdoc />
public sealed class MauiUiDispatcher : IUiDispatcher
{
    public bool IsDispatchRequired => !MainThread.IsMainThread;

    public Task DispatchAsync(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return MainThread.InvokeOnMainThreadAsync(action);
    }

    public Task DispatchAsync(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return MainThread.InvokeOnMainThreadAsync(action);
    }
}
