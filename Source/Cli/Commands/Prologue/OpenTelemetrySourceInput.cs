// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Prologue;

/// <summary>
/// Represents the OpenTelemetry capture source entered in the Prologue setup wizard.
/// </summary>
/// <param name="ServiceNames">The service names to capture spans for; empty captures every service.</param>
/// <param name="AttributeKeys">The span attribute keys whose values are captured.</param>
/// <param name="UpstreamHttp">The upstream OTLP/HTTP collector telemetry is forwarded to; empty for a terminal capture.</param>
/// <param name="UpstreamGrpc">The upstream OTLP/gRPC collector telemetry is forwarded to; empty for a terminal capture.</param>
public record OpenTelemetrySourceInput(
    IReadOnlyList<string> ServiceNames,
    IReadOnlyList<string> AttributeKeys,
    string UpstreamHttp,
    string UpstreamGrpc);
