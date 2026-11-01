// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// A host path made available inside a container.
/// </summary>
/// <param name="HostPath">The absolute path on the host.</param>
/// <param name="ContainerPath">The path inside the container.</param>
/// <param name="ReadOnly">Whether the container may only read it.</param>
public sealed record ContainerMount(string HostPath, string ContainerPath, bool ReadOnly = true)
{
    /// <summary>
    /// Builds the <c language="shell">-v</c> arguments that create the mount.
    /// </summary>
    /// <returns>The arguments.</returns>
    public IReadOnlyList<string> BuildArguments() => ["-v", $"{HostPath}:{ContainerPath}{(ReadOnly ? ":ro" : string.Empty)}"];
}
