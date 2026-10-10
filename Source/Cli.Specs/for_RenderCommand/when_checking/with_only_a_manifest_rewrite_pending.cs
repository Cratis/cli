// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_RenderCommand.when_checking;

[Collection(CliSpecsCollection.Name)]
public class with_only_a_manifest_rewrite_pending : given.a_check_command
{
    (int ExitCode, string Stdout, string Stderr) _output;
    string[] _before = [];

    async Task Establish()
    {
        await RenderFirst();

        // Still parses to the same manifest, but a real render rewrites the canonical bytes.
        await File.AppendAllTextAsync(ArtifactPublicationStorage.ManifestPath(_destination), " ");
        _before = Snapshot(_destination);
    }

    async Task Because() => _output = await Capture();

    [Fact] void should_report_changes_pending() => _output.ExitCode.ShouldEqual(ExitCodes.ChangesPending);
    [Fact] void should_report_every_artifact_unchanged() => Changes().All(_ => _.GetProperty("kind").GetString() == "unchanged").ShouldBeTrue();
    [Fact] void should_leave_every_file_and_directory_unchanged() => Snapshot(_destination).ShouldEqual(_before);

    JsonElement[] Changes()
    {
        using var json = JsonDocument.Parse(_output.Stdout);
        return [.. json.RootElement.GetProperty("publication").GetProperty("changes").EnumerateArray().Select(_ => _.Clone())];
    }
}
