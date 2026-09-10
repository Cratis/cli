// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli;

/// <summary>
/// Defines the xUnit test collection for CLI specs that share process-wide state such as the current directory,
/// environment variables, and Console.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public static class CliSpecsCollection
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public const string Name = "CliSpecs";
}
