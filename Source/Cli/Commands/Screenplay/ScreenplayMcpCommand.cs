// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Hosts the embedded Screenplay MCP protocol over standard input and output.
/// </summary>
[CommandEffect(CommandEffect.Local)]
[CliCommand("mcp", "Run the embedded Screenplay MCP server over stdio", Branch = typeof(ScreenplayBranch))]
[CliExample("screenplay", "mcp", ".cratis/screenplay")]
public sealed class ScreenplayMcpCommand : Command<ScreenplayMcpSettings>
{
    readonly IScreenplayMcpRunner _runner;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScreenplayMcpCommand"/> class.
    /// </summary>
    public ScreenplayMcpCommand() : this(new ScreenplayMcpRunner())
    {
    }

    internal ScreenplayMcpCommand(IScreenplayMcpRunner runner) => _runner = runner;

    /// <inheritdoc/>
    protected override int Execute(CommandContext context, ScreenplayMcpSettings settings, CancellationToken cancellationToken)
    {
        var args = new List<string>();
        if (settings.Path is not null) args.Add(settings.Path);
        if (settings.ProjectRoot is not null) args.AddRange(["--project-root", settings.ProjectRoot]);
        if (settings.ProjectRootEnvironment is not null) args.AddRange(["--project-root-env", settings.ProjectRootEnvironment]);
        return ScreenplayMcpInvocation.Run([.. args], _runner, Console.In, Console.Out, Console.Error, Directory.GetCurrentDirectory(), Environment.GetEnvironmentVariable);
    }
}
