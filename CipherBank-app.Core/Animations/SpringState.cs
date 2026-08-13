// <copyright file="SpringState.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Animations;

/// <summary>
/// One integration step of the snap spring: the new position and velocity.
/// </summary>
public readonly record struct SpringState(double Position, double Velocity);
