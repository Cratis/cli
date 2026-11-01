// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments.Secrets;

/// <summary>
/// Supplies the passphrase of the passphrase secrets provider. Implementations never persist it.
/// </summary>
public interface IPassphraseSource
{
    /// <summary>
    /// Gets the passphrase of the environment.
    /// </summary>
    /// <returns>The passphrase.</returns>
    string GetPassphrase();

    /// <summary>
    /// Gets a new passphrase, for creating an environment or changing its passphrase.
    /// </summary>
    /// <returns>The new passphrase.</returns>
    string GetNewPassphrase();
}
