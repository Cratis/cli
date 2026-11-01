// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments;

/// <summary>
/// The exception that is thrown when the passphrase does not match the environment.
/// </summary>
public class InvalidPassphraseException()
    : EnvironmentException("The passphrase is incorrect for this environment. Set CRATIS_PASSPHRASE or CRATIS_PASSPHRASE_FILE, or enter it when prompted.");
