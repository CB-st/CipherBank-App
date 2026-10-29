// <copyright file="IUiDispatcher.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Services;

/// <summary>Focused port for marshaling ViewModel work to the UI thread.</summary>
public interface IUiDispatcher
{
    bool IsDispatchRequired { get; }

    Task DispatchAsync(Action action);

    Task DispatchAsync(Func<Task> action);
}
