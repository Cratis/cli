// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments;

/// <summary>
/// Base exception for environment and secrets failures. Messages never contain secret values.
/// </summary>
/// <param name="message">The message.</param>
/// <param name="innerException">Optional inner exception.</param>
public class EnvironmentException(string message, Exception? innerException = null) : Exception(message, innerException);
