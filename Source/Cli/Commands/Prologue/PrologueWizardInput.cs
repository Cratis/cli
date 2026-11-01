// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Prologue;

/// <summary>
/// Represents everything entered across the Prologue setup wizard — the input
/// <see cref="PrologueConfigurationBuilder"/> turns into a <c language="csharp">cratis-prologue.json</c> configuration.
/// </summary>
/// <param name="PrologueId">The identity of the Prologue the captures will belong to.</param>
/// <param name="SqlServer">The SQL Server change-capture sources; empty when none were selected.</param>
/// <param name="Postgres">The PostgreSQL change-capture sources; empty when none were selected.</param>
/// <param name="Api">The API capture source; <see langword="null"/> when not selected.</param>
/// <param name="OpenTelemetry">The OpenTelemetry capture source; <see langword="null"/> when not selected.</param>
/// <param name="Output">Where the extractor writes captured data.</param>
/// <param name="Services">The named services of a system made of several services; <see langword="null"/> or empty when the system is captured as one service.</param>
public record PrologueWizardInput(
    Guid PrologueId,
    IReadOnlyList<SqlServerSourceInput> SqlServer,
    IReadOnlyList<PostgresSourceInput> Postgres,
    ApiSourceInput? Api,
    OpenTelemetrySourceInput? OpenTelemetry,
    CaptureOutputInput Output,
    IReadOnlyList<ServiceSourceInput>? Services = null)
{
    /// <summary>
    /// Gets the named services, empty when the system is captured as one service.
    /// </summary>
    public IReadOnlyList<ServiceSourceInput> NamedServices => Services ?? [];

    /// <summary>
    /// Gets a value indicating whether any service — the default one or a named one — captures HTTP commands
    /// through the reverse proxy.
    /// </summary>
    public bool CapturesApi => Api is not null || NamedServices.Any(service => service.Api is not null);
}
