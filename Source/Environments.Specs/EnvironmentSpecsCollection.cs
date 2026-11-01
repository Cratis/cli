// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments;

/// <summary>
/// Collection for specs that change process-wide state such as environment variables.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public static class EnvironmentSpecsCollection
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public const string Name = "EnvironmentSpecs";
}
