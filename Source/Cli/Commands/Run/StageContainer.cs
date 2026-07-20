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
    /// The path inside the container the folder of Screenplay files is mounted at.
    /// </summary>
    public const string MountPath = "/eventmodel";

    /// <summary>
    /// Builds the argument list for <c>docker run</c> that launches the Stage container with the given
    /// folder mounted and the Stage API published on the host.
    /// </summary>
    /// <param name="path">The absolute path to the folder of Screenplay files to mount.</param>
    /// <param name="tag">The image tag to run.</param>
    /// <param name="hostPort">The host port to publish the Stage API on.</param>
    /// <returns>The ordered argument list to pass to the <c>docker</c> executable.</returns>
    public static IReadOnlyList<string> BuildRunArguments(string path, string tag, int hostPort) =>
    [
        "run",
        "--rm",
        "-p",
        $"{hostPort}:{ApiPort}",
        "-v",
        $"{path}:{MountPath}",
        $"{Image}:{tag}"
    ];
}
