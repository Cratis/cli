// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// The exception that is thrown when a command needs a Studio connection this machine does not have.
/// </summary>
/// <param name="message">What is missing and how to set it up.</param>
public sealed class StudioNotConnected(string message) : Exception(message);
