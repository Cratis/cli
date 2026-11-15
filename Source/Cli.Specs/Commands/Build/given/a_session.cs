// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.given;

public static class a_session
{
    public static readonly string Root = Path.Combine(Path.GetTempPath(), "cratis-build-session");
    public static readonly string Project = Path.Combine(Root, "project");
    public static readonly string Workspace = Path.Combine(Root, "workspace");
    public const string StudioToken = "studio-bearer-token-value";

    public static BuildAgentSession With(bool mayChangeTools = true, List<string>? log = null) => new(
        Project,
        Workspace,
        "Stack instructions",
        null,
        null,
        BuildSessionTools.For(Path.Combine(Workspace, "model"), "https://mcp.cratis.studio", StudioToken),
        new BuildPermissions(Project, ["src/**"], ["npm test"], [Workspace], [Path.Combine(Workspace, "outcome.json")]),
        line => log?.Add(line))
    {
        MayChangeTools = mayChangeTools
    };
}
