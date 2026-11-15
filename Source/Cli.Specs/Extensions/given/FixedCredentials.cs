// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Extensions;

/// <summary>
/// Hands out one credential, and counts how often it was asked for.
/// </summary>
/// <param name="credential">The credential, or <see langword="null"/> to connect anonymously.</param>
class FixedCredentials(RegistryCredential? credential = null) : IRegistryCredentials
{
    public int Asked { get; private set; }

    public Task<RegistryCredential?> For(string host, CancellationToken cancellationToken)
    {
        Asked++;
        return Task.FromResult(credential);
    }
}
