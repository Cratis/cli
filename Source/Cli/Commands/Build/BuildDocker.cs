// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Plans running <c language="csharp">cratis build run</c> inside a packaged builder image, so a machine with nothing but Docker
/// and the Cratis CLI can run the same builder Studio runs in the cloud. The builder's credential never appears on a command
/// line: Docker is told to take it from this process's environment.
/// </summary>
public static class BuildDocker
{
    /// <summary>The folder the project is mounted at inside the image.</summary>
    public const string Workspace = "/workspace";

    /// <summary>The image the packaged Cratis builder is published as, without a tag.</summary>
    public const string DefaultImage = "ghcr.io/cratis/builder";

    /// <summary>The variables that carry the builder's connection into the container.</summary>
    public static readonly IReadOnlyList<string> ConnectionVariables =
    [
        StudioConnection.UrlVariable,
        StudioConnection.OrganizationVariable,
        StudioConnection.BuilderIdVariable,
        StudioConnection.BuilderTokenVariable
    ];

    /// <summary>The variables an agent signs in with, passed through when they are set so an agent in the container is as signed in as one outside it.</summary>
    public static readonly IReadOnlyList<string> AgentVariables =
    [
        "ANTHROPIC_API_KEY",
        "CLAUDE_CODE_OAUTH_TOKEN",
        "OPENAI_API_KEY",
        "GEMINI_API_KEY",
        "GITHUB_TOKEN",
        "GH_TOKEN"
    ];

    /// <summary>
    /// Gets the image to run: the one asked for, or the packaged builder at this CLI's version.
    /// </summary>
    /// <param name="image">The image asked for, or <see langword="null"/>.</param>
    /// <param name="version">This CLI's version.</param>
    /// <returns>The image reference.</returns>
    public static string ImageFor(string? image, string version) =>
        string.IsNullOrWhiteSpace(image) ? $"{DefaultImage}:{(version.Length == 0 ? "latest" : version.Split('+')[0])}" : image;

    /// <summary>
    /// Gets the arguments that start the builder in the image.
    /// </summary>
    /// <param name="settings">What was asked for.</param>
    /// <param name="project">The project folder on this machine.</param>
    /// <param name="image">The image to run.</param>
    /// <param name="present">Tells whether an environment variable is set on this machine.</param>
    /// <param name="home">The home folder, for Git's configuration.</param>
    /// <param name="user">The user and group to run as, or <see langword="null"/> to leave the image's own.</param>
    /// <returns>The arguments after <c language="csharp">docker</c>.</returns>
    public static IReadOnlyList<string> Arguments(BuildRunSettings settings, string project, string image, Func<string, bool> present, string home, string? user)
    {
        var arguments = new List<string> { "run", "--rm", "-i" };
        if (user is not null) arguments.AddRange(["--user", user]);
        arguments.AddRange(["-v", $"{project}:{Workspace}", "-w", Workspace]);
        if (File.Exists(Path.Combine(home, ".gitconfig"))) arguments.AddRange(["-v", $"{Path.Combine(home, ".gitconfig")}:/etc/gitconfig:ro"]);
        arguments.AddRange(["-e", "HOME=/tmp", "-e", "GIT_CONFIG_COUNT=1", "-e", "GIT_CONFIG_KEY_0=safe.directory", "-e", $"GIT_CONFIG_VALUE_0={Workspace}"]);
        foreach (var name in ConnectionVariables.Concat(AgentVariables.Where(present))) arguments.AddRange(["-e", name]);
        arguments.Add(image);
        arguments.AddRange(Inner(settings));
        return arguments;
    }

    /// <summary>
    /// Gets the command the builder runs inside the image: the same <c language="csharp">cratis build run</c>, aimed at the mounted project.
    /// </summary>
    /// <param name="settings">What was asked for.</param>
    /// <returns>The arguments after <c language="csharp">cratis</c>.</returns>
    public static IReadOnlyList<string> Inner(BuildRunSettings settings)
    {
        var arguments = new List<string> { "build", "run", "--project", Workspace };
        void Option(string name, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value)) arguments.AddRange([name, value]);
        }

        Option("--agent", settings.Agent);
        Option("--agent-command", settings.AgentCommand);
        Option("--exec", settings.Exec);
        Option("--level", settings.Level);
        Option("--target", settings.Target);
        Option("--ai", settings.Ai);
        foreach (var verify in settings.Verify) Option("--verify", verify);
        if (settings.Once) arguments.Add("--once");
        if (settings.NoPush) arguments.Add("--no-push");
        if (settings.NoGuidance) arguments.Add("--no-guidance");
        return arguments;
    }
}
