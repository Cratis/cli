// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// The exception that is thrown when a document is not a build order.
/// </summary>
/// <param name="reason">Why it is not an order.</param>
public sealed class InvalidBuildOrder(string reason) : Exception(reason);
