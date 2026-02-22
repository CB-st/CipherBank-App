// <copyright file="GlobalSuppressions.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Diagnostics.CodeAnalysis;

// Test methods use underscores in naming (Method_When_Then convention)
[assembly: SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test method naming convention")]
