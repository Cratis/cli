// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// The URLs a Stage session is reachable on from the host.
/// </summary>
/// <param name="Api">The Stage API.</param>
/// <param name="ApiReference">The interactive API reference for the compiled event model.</param>
/// <param name="Workbench">The Chronicle Workbench.</param>
public record StageEndpoints(string Api, string ApiReference, string Workbench)
{
    /// <summary>
    /// The user to sign in to the Chronicle Workbench with — the Stage image ships with development credentials.
    /// </summary>
    public const string WorkbenchUser = "admin";

    /// <summary>
    /// The password to sign in to the Chronicle Workbench with.
    /// </summary>
    public const string WorkbenchPassword = "ChangeMeNow!";

    /// <summary>
    /// Resolves the endpoints for the host ports the container's ports are published on.
    /// </summary>
    /// <param name="port">The host port the Stage API is published on.</param>
    /// <param name="workbenchPort">The host port the Chronicle Workbench is published on.</param>
    /// <returns>The resolved <see cref="StageEndpoints"/>.</returns>
    public static StageEndpoints For(int port, int workbenchPort) =>
        new(
            $"http://localhost:{port}",
            $"http://localhost:{port}/scalar/v1",
            $"https://localhost:{workbenchPort}");
}
