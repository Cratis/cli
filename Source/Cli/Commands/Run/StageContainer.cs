// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Stage.Contracts.Rendering;

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
    /// The tag used when the Stage version cannot be established.
    /// </summary>
    public const string FallbackTag = "latest";

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
    /// The path inside the container a single Screenplay file is mounted at, and handed to the Stage as its input.
    /// </summary>
    public const string FileMountPath = "/eventmodel/input.play";

    /// <summary>
    /// The prefix of the name the container is given, so a running sandbox is recognizable in <c language="csharp">docker ps</c>
    /// and can be stopped by name.
    /// </summary>
    public const string NamePrefix = "cratis-stage-";

    /// <summary>
    /// The tag used when none is asked for: the Stage release this CLI renders with.
    /// </summary>
    /// <remarks>
    /// The CLI plans artifacts with a specific version of the Stage rendering packages, and the container is
    /// what reads them. Defaulting to <c language="csharp">latest</c> pairs a known renderer with whatever was published most
    /// recently, so the pair drifts apart on somebody else's release rather than on an upgrade here - and the
    /// symptom arrives at a user who changed nothing.
    /// <para>
    /// Every Stage release publishes an exact tag, so the version the renderer came from is always a tag that
    /// exists. <c language="csharp">--tag</c> still overrides it, which is how a newer or older sandbox is tried on purpose.
    /// </para>
    /// </remarks>
    public static string DefaultTag { get; } = ResolveDefaultTag();

    /// <summary>
    /// Generates a unique name for a container, so several sandboxes can run side by side.
    /// </summary>
    /// <returns>The container name.</returns>
    public static string GenerateName() => $"{NamePrefix}{Guid.NewGuid():N}"[..(NamePrefix.Length + 8)];

    /// <summary>
    /// Builds the argument list for <c language="csharp">docker run</c> that launches the Stage container with the given
    /// folder mounted read-only and the Stage API and Chronicle Workbench published on the host.
    /// </summary>
    /// <param name="path">The absolute path to the folder of Screenplay files to mount.</param>
    /// <param name="tag">The image tag to run.</param>
    /// <param name="hostPort">The host port to publish the Stage API on.</param>
    /// <param name="workbenchHostPort">The host port to publish the Chronicle Workbench on.</param>
    /// <param name="name">The name to give the container.</param>
    /// <returns>The ordered argument list to pass to the <c language="csharp">docker</c> executable.</returns>
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
        $"{path}:{MountPath}:ro",
        $"{Image}:{tag}"
    ];

    /// <summary>
    /// Builds the argument list for <c language="csharp">docker run</c> that launches the Stage container with only the
    /// given Screenplay file mounted read-only, and the Stage API and Chronicle Workbench published on the host.
    /// </summary>
    /// <param name="path">The absolute path to the Screenplay file to mount.</param>
    /// <param name="tag">The image tag to run.</param>
    /// <param name="hostPort">The host port to publish the Stage API on.</param>
    /// <param name="workbenchHostPort">The host port to publish the Chronicle Workbench on.</param>
    /// <param name="name">The name to give the container.</param>
    /// <returns>The ordered argument list to pass to the <c language="csharp">docker</c> executable.</returns>
    /// <remarks>
    /// The file is mounted at a fixed path rather than its own name, and that path is passed after the image as the
    /// Stage's input. The folder the file sits in is never mounted, so sibling files stay out of the container.
    /// </remarks>
    public static IReadOnlyList<string> BuildRunArgumentsForFile(string path, string tag, int hostPort, int workbenchHostPort, string name) =>
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
        $"{path}:{FileMountPath}:ro",
        $"{Image}:{tag}",
        FileMountPath
    ];

    /// <summary>
    /// Builds the argument list for stopping a running container by name.
    /// </summary>
    /// <param name="name">The name of the container to stop.</param>
    /// <returns>The ordered argument list to pass to the <c language="csharp">docker</c> executable.</returns>
    public static IReadOnlyList<string> BuildStopArguments(string name) => ["stop", name];

    /// <summary>
    /// Builds the argument list for asking Docker whether the container is still running.
    /// </summary>
    /// <param name="name">The name of the container to ask about.</param>
    /// <returns>The ordered argument list to pass to the <c language="csharp">docker</c> executable.</returns>
    /// <remarks>
    /// The name is anchored so a container whose name merely contains this one cannot answer for it.
    /// </remarks>
    public static IReadOnlyList<string> BuildIsRunningArguments(string name) =>
        ["ps", "--quiet", "--filter", $"name=^{name}$"];

    /// <summary>
    /// Reads Docker's answer to <see cref="BuildIsRunningArguments"/>.
    /// </summary>
    /// <param name="output">What Docker wrote.</param>
    /// <returns>True when the container is still running.</returns>
    /// <remarks>
    /// An id means it is up; nothing at all means it is gone. Asking Docker is the only thing that actually
    /// answers the question - the Docker client this CLI started is a different process from the container it
    /// asked for, and it can exit while the container keeps running.
    /// </remarks>
    public static bool IsRunningFrom(string output) => !string.IsNullOrWhiteSpace(output);

    /// <summary>
    /// Reads the version of the Stage packages this CLI was built against.
    /// </summary>
    /// <returns>The tag to run by default.</returns>
    /// <remarks>
    /// The informational version carries build metadata after a <c language="csharp">+</c>, which is not part of the tag. A
    /// prerelease version has no published image, so it falls back rather than asking Docker for a tag that
    /// cannot exist - a locally built CLI should still be able to run a sandbox.
    /// </remarks>
    static string ResolveDefaultTag()
    {
        var informational = typeof(ArtifactRenderPlan).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (string.IsNullOrWhiteSpace(informational)) return FallbackTag;

        var version = informational.Split('+')[0];
        return version.Contains('-', StringComparison.Ordinal) || !System.Version.TryParse(version, out _)
            ? FallbackTag
            : version;
    }
}
