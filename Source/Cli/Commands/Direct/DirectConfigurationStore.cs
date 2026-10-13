// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Direct;

/// <summary>
/// Serializes Direct configuration read-modify-write operations across processes.
/// The configuration lock is always acquired before any per-target refresh lock.
/// </summary>
/// <param name="configurationLock">Cross-process lock shared by login and logout.</param>
/// <param name="load">Loads the latest configuration.</param>
/// <param name="save">Publishes the updated configuration.</param>
internal sealed class DirectConfigurationStore(IDirectRefreshLock configurationLock, Func<CliConfiguration>? load = null, Action<CliConfiguration>? save = null)
{
    internal async Task<T> Update<T>(Func<CliConfiguration, Action, Task<T>> update, CancellationToken cancellationToken)
    {
        await using var held = await configurationLock.Acquire("configuration", cancellationToken);
        var config = (load ?? CliConfiguration.Load)();
        return await update(config, () =>
        {
            if (save is null)
            {
                config.Save();
            }
            else
            {
                save(config);
            }
        });
    }
}
