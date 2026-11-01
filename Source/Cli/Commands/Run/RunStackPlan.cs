// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// A file a run writes before it starts any container.
/// </summary>
/// <param name="RelativePath">The path under the run directory.</param>
/// <param name="Content">What the file holds. May be a secret; it is only ever written to the file.</param>
public sealed record PlannedFile(string RelativePath, string Content)
{
    /// <inheritdoc/>
    public override string ToString() => $"PlannedFile {{ RelativePath = {RelativePath} }}";
}

/// <summary>
/// A container of the stack that starts after the Stage.
/// </summary>
/// <param name="Role">What the container is for, such as <c language="text">authproxy</c>.</param>
/// <param name="Name">The container name.</param>
/// <param name="Image">The image reference.</param>
/// <param name="Arguments">The arguments for <c language="shell">docker run</c>. They never carry a secret.</param>
public sealed record PlannedContainer(string Role, string Name, string Image, IReadOnlyList<string> Arguments);

/// <summary>
/// One service of the stack and where the user reaches it.
/// </summary>
/// <param name="Name">The label shown for it.</param>
/// <param name="Url">Where it is reached from the host.</param>
public sealed record RunService(string Name, string Url);

/// <summary>
/// What a run with authentication starts: the files it writes, the Stage's networking and the containers that follow the Stage.
/// </summary>
/// <param name="Network">The Docker network of the run.</param>
/// <param name="Stage">How the Stage joins the network.</param>
/// <param name="Directories">The directories created under the run directory.</param>
/// <param name="Files">The files written under the run directory.</param>
/// <param name="Containers">The containers started after the Stage, in start order. They are stopped in reverse order.</param>
/// <param name="Services">The services the user can open.</param>
/// <param name="EntryUrl">The address of AuthProxy, which is the application's entry point.</param>
/// <param name="MailFolder">The folder onboarding emails are written to, or null when the environment names a real mail server or there is no onboarding.</param>
public sealed record RunStackPlan(
    string Network,
    StageRuntimeOptions Stage,
    IReadOnlyList<string> Directories,
    IReadOnlyList<PlannedFile> Files,
    IReadOnlyList<PlannedContainer> Containers,
    IReadOnlyList<RunService> Services,
    string EntryUrl,
    string? MailFolder)
{
    /// <summary>
    /// Gets the arguments that create the network.
    /// </summary>
    public IReadOnlyList<string> CreateNetworkArguments => ["network", "create", Network];

    /// <summary>
    /// Gets the arguments that remove the network.
    /// </summary>
    public IReadOnlyList<string> RemoveNetworkArguments => ["network", "rm", Network];
}
