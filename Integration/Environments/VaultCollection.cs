// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments.Integration;

/// <summary>
/// Shares one Vault dev container between the integration specs.
/// </summary>
[CollectionDefinition(Name)]
public class VaultCollection : ICollectionFixture<VaultContainerFixture>
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public const string Name = "Vault";
}
