// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle;

/// <summary>
/// A token provider that returns a pre-configured static token (e.g. a cached login access token).
/// </summary>
/// <param name="token">The access token to return for all calls.</param>
sealed class StaticTokenProvider(string token) : ITokenProvider
{
    /// <inheritdoc/>
    public Task<string?> GetAccessToken(CancellationToken cancellationToken = default) => Task.FromResult<string?>(token);

    /// <inheritdoc/>
    public Task<string?> Refresh(CancellationToken cancellationToken = default) => Task.FromResult<string?>(token);
}
