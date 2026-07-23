// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Prologue;

/// <summary>
/// Represents one PostgreSQL change-capture source entered in the Prologue setup wizard.
/// </summary>
/// <param name="Name">The logical name identifying the source in captures.</param>
/// <param name="ConnectionString">The connection string to the PostgreSQL database.</param>
public record PostgresSourceInput(string Name, string ConnectionString);
