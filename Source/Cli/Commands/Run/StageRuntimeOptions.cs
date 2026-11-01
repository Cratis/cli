// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// How the Stage container joins the other services of a run: the Docker network it lives on, the interface its ports are
/// published on, and the environment and mounts the application is given.
/// </summary>
/// <param name="Network">The name of the Docker network of the run.</param>
/// <param name="Alias">The name the other containers reach the Stage by.</param>
/// <param name="PublishAddress">The host address the Stage's ports are published on, so the unauthenticated Stage is not exposed beyond the machine.</param>
/// <param name="Environment">The plain environment variables the Stage is given. Secrets never travel here; they are mounted as files.</param>
/// <param name="Mounts">The additional mounts, such as the directory holding the environment's values.</param>
public sealed record StageRuntimeOptions(
    string Network,
    string Alias,
    string PublishAddress,
    IReadOnlyList<KeyValuePair<string, string>> Environment,
    IReadOnlyList<ContainerMount> Mounts)
{
    /// <summary>
    /// Gets the prefix put in front of a published port.
    /// </summary>
    public string PublishPrefix => $"{PublishAddress}:";

    /// <summary>
    /// Builds the arguments that join the container to the network of the run.
    /// </summary>
    /// <returns>The arguments.</returns>
    public IReadOnlyList<string> BuildNetworkArguments() => ["--network", Network, "--network-alias", Alias];

    /// <summary>
    /// Builds the environment and mount arguments.
    /// </summary>
    /// <returns>The arguments.</returns>
    public IReadOnlyList<string> BuildArguments() =>
    [
        .. Environment.SelectMany(pair => new[] { "-e", $"{pair.Key}={pair.Value}" }),
        .. Mounts.SelectMany(mount => mount.BuildArguments())
    ];
}
