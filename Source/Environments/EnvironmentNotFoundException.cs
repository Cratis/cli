// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments;

/// <summary>
/// The exception that is thrown when an environment file does not exist.
/// </summary>
/// <param name="name">The environment name.</param>
public class EnvironmentNotFoundException(string name)
    : EnvironmentException($"Environment '{name}' does not exist. Create it with: cratis env init {name}");
