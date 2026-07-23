// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Prologue;

/// <summary>
/// Represents the API capture source entered in the Prologue setup wizard — the extractor sits in front of the
/// system as a reverse proxy and observes the state-changing HTTP commands flowing through it.
/// </summary>
/// <param name="BasePath">The base path the proxy forwards (for example <c>/api</c>).</param>
/// <param name="Destination">The address of the system being captured that proxied requests are forwarded to.</param>
public record ApiSourceInput(string BasePath, string Destination);
