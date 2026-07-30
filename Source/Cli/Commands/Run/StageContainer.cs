// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// Describes the Stage Docker container the run command launches and builds its invocation arguments.
/// </summary>
public static class StageContainer
{
    /// <summary>
    /// The Docker image name for the Stage sandbox.
    /// </summary>
    public const string Image = "cratis/stage";

    /// <summary>
    /// The port the Stage API listens on inside the container.
    /// </summary>
    public const int ApiPort = 9090;

    /// <summary>
    /// The port the bundled Chronicle kernel serves the Chronicle Workbench on inside the container.
    /// </summary>
    public const int WorkbenchPort = 35000;

    /// <summary>
    /// The path inside the container the folder of Screenplay files is mounted at.
    /// </summary>
    public const string MountPath = "/eventmodel";

    /// <summary>
    /// The prefix of the name the container is given, so a running sandbox is recognizable in <c>docker ps</c>
    /// and can be stopped by name.
    /// </summary>
    public const string NamePrefix = "cratis-stage-";

    /// <summary>
    /// Generates a unique name for a container, so several sandboxes can run side by side.
    /// </summary>
    /// <returns>The container name.</returns>
    public static string GenerateName() => $"{NamePrefix}{Guid.NewGuid():N}"[..(NamePrefix.Length + 8)];

    /// <summary>
    /// Builds the argument list for <c>docker run</c> that launches the Stage container with the given
    /// folder mounted and the Stage API and Chronicle Workbench published on the host.
    /// </summary>
    /// <param name="path">The absolute path to the folder of Screenplay files to mount.</param>
    /// <param name="tag">The image tag to run.</param>
    /// <param name="hostPort">The host port to publish the Stage API on.</param>
    /// <param name="workbenchHostPort">The host port to publish the Chronicle Workbench on.</param>
    /// <param name="name">The name to give the container.</param>
    /// <returns>The ordered argument list to pass to the <c>docker</c> executable.</returns>
    public static IReadOnlyList<string> BuildRunArguments(string path, string tag, int hostPort, int workbenchHostPort, string name) =>
    [
        "run",
        "--rm",
        "--name",
        name,
        "-p",
        $"{hostPort}:{ApiPort}",
        "-p",
        $"{workbenchHostPort}:{WorkbenchPort}",
        "-v",
        $"{path}:{MountPath}",
        $"{Image}:{tag}"
    ];

    /// <summary>
    /// Builds the argument list for stopping a running container by name.
    /// </summary>
    /// <param name="name">The name of the container to stop.</param>
    /// <returns>The ordered argument list to pass to the <c>docker</c> executable.</returns>
    public static IReadOnlyList<string> BuildStopArguments(string name) => ["stop", name];
}
