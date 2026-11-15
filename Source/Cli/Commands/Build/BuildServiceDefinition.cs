// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// What an always-on builder runs as a user service: <c language="shell">cratis build run</c> in one project, with the options it
/// was installed with. Credentials never reach it: the builder token stays in the credential store and the service is given only
/// the <c language="shell">PATH</c> of the shell that installed it.
/// </summary>
/// <param name="Name">The service's name, unique per project: <c language="shell">cratis-build-&lt;folder&gt;-&lt;hash&gt;</c>.</param>
/// <param name="Program">The full path of the <c language="shell">cratis</c> program.</param>
/// <param name="Arguments">The arguments: <c language="shell">build run</c> and the options it was installed with.</param>
/// <param name="Project">The project folder it builds in.</param>
/// <param name="SearchPath">The <c language="shell">PATH</c> it runs with, so it finds the agent, Git and the kit's tools.</param>
public sealed record BuildServiceDefinition(string Name, string Program, IReadOnlyList<string> Arguments, string Project, string SearchPath)
{
    /// <summary>
    /// Gets the name of a project's service.
    /// </summary>
    /// <param name="project">The project folder, resolved.</param>
    /// <returns>The name.</returns>
    public static string NameFor(string project)
    {
        var folder = new string([.. Path.GetFileName(project.TrimEnd(Path.DirectorySeparatorChar)).ToLowerInvariant()
            .Select(character => char.IsAsciiLetterOrDigit(character) ? character : '-')]).Trim('-');
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(project)))[..8];
        return $"cratis-build-{(folder.Length == 0 ? "project" : folder[..Math.Min(folder.Length, 40)])}-{hash}";
    }

    /// <summary>
    /// Describes the service for a project from the options of <c language="shell">cratis build service install</c>.
    /// </summary>
    /// <param name="settings">The options.</param>
    /// <param name="program">The full path of the <c language="shell">cratis</c> program.</param>
    /// <param name="prefix">The arguments that come before the CLI's own, such as its assembly when it runs through <c language="shell">dotnet</c>.</param>
    /// <param name="searchPath">The <c language="shell">PATH</c> to run with.</param>
    /// <returns>The service.</returns>
    public static BuildServiceDefinition For(BuildServiceSettings settings, string program, IReadOnlyList<string> prefix, string searchPath)
    {
        var project = settings.ProjectPath;
        var arguments = new List<string>(prefix) { "build", "run", "--project", project };
        if (settings.Agent is { } agent) arguments.AddRange(["--agent", agent]);
        if (settings.AgentCommand is { } agentCommand) arguments.AddRange(["--agent-command", agentCommand]);
        if (settings.Exec is { } exec) arguments.AddRange(["--exec", exec]);
        if (settings.Kit is { } kit) arguments.AddRange(["--kit", Path.GetFullPath(kit)]);
        foreach (var verify in settings.Verify) arguments.AddRange(["--verify", verify]);
        if (settings.NoGuidance) arguments.Add("--no-guidance");
        if (settings.NoPush) arguments.Add("--no-push");
        arguments.AddRange(["--output", "plain"]);
        return new(NameFor(project), program, arguments, project, searchPath);
    }
}
