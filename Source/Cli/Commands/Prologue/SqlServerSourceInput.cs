// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Prologue;

/// <summary>
/// Represents one SQL Server change-capture source entered in the Prologue setup wizard.
/// </summary>
/// <param name="Name">The logical name identifying the source in captures.</param>
/// <param name="ConnectionString">The connection string to the SQL Server database.</param>
/// <param name="Tables">The tables to capture changes for; empty captures every user table.</param>
public record SqlServerSourceInput(string Name, string ConnectionString, IReadOnlyList<string> Tables);
