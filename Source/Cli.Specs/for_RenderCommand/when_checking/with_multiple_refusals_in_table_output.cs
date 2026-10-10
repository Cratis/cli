// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_RenderCommand.when_checking;

[Collection(CliSpecsCollection.Name)]
public class with_multiple_refusals_in_table_output : given.a_check_command
{
    (int ExitCode, string Stdout, string Stderr) _output;
    string[] _paths = [];

    async Task Establish()
    {
        await RenderFirst();
        _paths = [.. _artifactPlan.Artifacts.Take(2).Select(_ => _.RelativePath)];
        foreach (var path in _paths)
        {
            await File.WriteAllTextAsync(ArtifactPublicationStorage.ArtifactPath(_destination, path), "edited bytes");
        }

        _settings.Output = OutputFormats.Table;
    }

    async Task Because() => _output = await Capture();

    [Fact] void should_fail_validation() => _output.ExitCode.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_summarize_both_refusals() => _output.Stdout.ShouldContain("2 refusal(s)");
    [Fact] void should_list_both_paths_with_their_reasons() => _paths.All(path => _output.Stdout.Contains($"refused: {path} — Managed artifact '{path}' was modified by the user; pass --force to replace it.", StringComparison.Ordinal)).ShouldBeTrue();
}
