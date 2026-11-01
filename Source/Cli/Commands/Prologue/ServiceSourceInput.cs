// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Prologue;

/// <summary>
/// Represents one named service of a system made of several services, as entered in the Prologue setup wizard —
/// the sources that belong to that service only.
/// </summary>
/// <param name="Name">The name of the service; also the reverse-proxy cluster its HTTP commands are routed to.</param>
/// <param name="SqlServer">The SQL Server change-capture sources of the service; empty when none were selected.</param>
/// <param name="Postgres">The PostgreSQL change-capture sources of the service; empty when none were selected.</param>
/// <param name="Api">The API capture source of the service; <see langword="null"/> when not selected.</param>
/// <param name="TelemetryServiceNames">The OpenTelemetry <c language="csharp">service.name</c> values of the service; empty means the service name itself.</param>
public record ServiceSourceInput(
    string Name,
    IReadOnlyList<SqlServerSourceInput> SqlServer,
    IReadOnlyList<PostgresSourceInput> Postgres,
    ApiSourceInput? Api,
    IReadOnlyList<string> TelemetryServiceNames);
