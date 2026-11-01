// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments;

/// <summary>
/// The exception that is thrown when an environment name is not a safe file name.
/// </summary>
/// <param name="name">The offending name.</param>
public class InvalidEnvironmentNameException(string name)
    : EnvironmentException($"'{name}' is not a valid environment name. Use letters, digits, '.', '_' and '-', starting with a letter or digit, at most 64 characters.");
