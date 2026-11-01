// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments;

/// <summary>
/// The exception that is thrown when a secret cannot be decrypted or fetched.
/// </summary>
/// <param name="message">The message, which never contains the value.</param>
/// <param name="innerException">Optional inner exception.</param>
public class SecretNotAvailableException(string message, Exception? innerException = null) : EnvironmentException(message, innerException);
