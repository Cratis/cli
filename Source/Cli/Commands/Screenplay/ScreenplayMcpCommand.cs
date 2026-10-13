// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Hosts the embedded Screenplay MCP protocol over standard input and output.
/// </summary>
[LlmDescription("Runs the embedded Screenplay authoring and event model MCP server over standard input and output. Protocol runs write only JSON-RPC to stdout, report startup errors to stderr, and do not check for updates. Select at most one of [PATH], --project-root, or --project-root-env.")]
[CommandEffect(CommandEffect.Local)]
[CliCommand("mcp", "Run Screenplay authoring and event model views over MCP stdio", Branch = typeof(ScreenplayBranch))]
[CliExample("screenplay", "mcp", ".cratis/screenplay")]
[LlmOption("[PATH]", "string", "Explicit model directory. Defaults to the model already in the project, else its Source or src folder, else a new Screenplay folder.")]
[LlmOption("--project-root", "string", "Resolve the model root from this project's .cratis/ai.json.")]
[LlmOption("--project-root-env", "string", "Read the project directory from a host-provided environment variable.")]
public sealed class ScreenplayMcpCommand : Command<ScreenplayMcpSettings>
{
    readonly IScreenplayMcpRunner _runner;
    readonly TextReader _input;
    readonly TextWriter _output;
    readonly TextWriter _error;
    readonly string _workingDirectory;
    readonly Func<string, string?> _environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScreenplayMcpCommand"/> class.
    /// </summary>
    public ScreenplayMcpCommand()
        : this(new ScreenplayMcpRunner(), Console.In, Console.Out, Console.Error, Directory.GetCurrentDirectory(), Environment.GetEnvironmentVariable)
    {
    }

    internal ScreenplayMcpCommand(IScreenplayMcpRunner runner, TextReader input, TextWriter output, TextWriter error, string workingDirectory, Func<string, string?> environment)
    {
        _runner = runner;
        _input = input;
        _output = output;
        _error = error;
        _workingDirectory = workingDirectory;
        _environment = environment;
    }

    /// <inheritdoc/>
    public override int Execute(CommandContext context, ScreenplayMcpSettings settings, CancellationToken cancellationToken)
    {
        if (context.Remaining.Parsed.Count > 0 || context.Remaining.Raw.Count > 0)
        {
            var option = context.Remaining.Raw.Count > 0 ? context.Remaining.Raw[0] : context.Remaining.Parsed.First().Key;
            _error.WriteLine($"Unknown option '{option}'. Run 'cratis screenplay mcp --help' for usage.");
            return ExitCodes.ValidationError;
        }

        return ScreenplayMcpInvocation.Run(settings.Path, settings.ProjectRoot, settings.ProjectRootEnvironment, _runner, _input, _output, _error, _workingDirectory, _environment);
    }
}
