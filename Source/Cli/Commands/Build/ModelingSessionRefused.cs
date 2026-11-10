// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// The exception that is thrown when Studio does not accept what a modeling session asked of it.
/// </summary>
/// <param name="message">What was refused, and why.</param>
public sealed class ModelingSessionRefused(string message) : Exception(message);
