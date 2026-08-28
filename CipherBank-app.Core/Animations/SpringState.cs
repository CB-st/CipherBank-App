// <copyright file="SpringState.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Animations;

/// <summary>
/// One integration step of the snap spring: the new position and velocity.
/// </summary>
public readonly record struct SpringState(double Position, double Velocity);
