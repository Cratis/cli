// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments;

/// <summary>
/// The outcome of resolving which environment is in effect.
/// </summary>
/// <param name="Name">The environment name.</param>
/// <param name="Source">Where the name came from.</param>
public record SelectedEnvironment(string Name, SelectionSource Source);
