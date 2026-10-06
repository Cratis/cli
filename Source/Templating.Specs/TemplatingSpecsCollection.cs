// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Templating.Specs;

/// <summary>
/// Defines the xUnit collection for templating specs that change the process-wide current directory.
/// These specs must not overlap each other or other collections that read the current directory.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public static class TemplatingSpecsCollection
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public const string Name = "TemplatingSpecs";
}
