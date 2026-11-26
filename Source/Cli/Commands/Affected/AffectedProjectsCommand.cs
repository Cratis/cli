// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Affected;

namespace Cratis.Cli.Commands.Affected;

/// <summary>
/// Lists the test projects of a repository, for splitting a merge-time run across jobs.
/// </summary>
[LlmDescription("Lists the test projects of the repository at a ref, as the configuration in .cratis/affected.json sees them. A merge-time workflow uses it to build one job per project and pass each to 'affected run --projects'.")]
[CommandEffect(CommandEffect.Local)]
[CliCommand("projects", "List the test projects", Branch = typeof(AffectedBranch))]
[CliExample("affected", "projects")]
[CliExample("affected", "projects", "--output", "json-compact")]
[LlmOutputAdvice("json", "One entry per test project with its repository relative path.")]
[LlmOption("--to", "string", "Ref to read the projects from (default HEAD)")]
public sealed class AffectedProjectsCommand : AsyncCommand<AffectedProjectsSettings>
{
    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, AffectedProjectsSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        try
        {
            var git = GitRepository.Open(Directory.GetCurrentDirectory());
            var graph = ProjectGraph.Load(git, git.ResolveCommit(settings.To), AffectedConfiguration.Load(git.Root));
            var projects = graph.Projects.Values
                .Where(project => project.IsTestProject)
                .Select(project => project.Path)
                .Order(StringComparer.Ordinal)
                .ToList();
            OutputFormatter.Write(format, projects, ["Test project"], path => [path], path => path);
            return Task.FromResult(ExitCodes.Success);
        }
        catch (Exception exception) when (exception is AffectedException or ArgumentException)
        {
            OutputFormatter.WriteError(format, exception.Message, errorCode: ExitCodes.ValidationErrorCode);
            return Task.FromResult(ExitCodes.ValidationError);
        }
    }
}
