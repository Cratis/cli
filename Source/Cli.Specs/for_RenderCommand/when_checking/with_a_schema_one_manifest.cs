// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Cli.Commands.Render.Publication;
using Cratis.Screenplay.CanonicalCorpus;

namespace Cratis.Cli.for_RenderCommand.when_checking;

[Collection(CliSpecsCollection.Name)]
public class with_a_schema_one_manifest : given.a_check_command
{
    (int ExitCode, string Stdout, string Stderr) _checked;
    string[] _before = [];

    async Task Establish()
    {
        _planning.Plan(Arg.Any<ScreenplayRenderRequest>(), Arg.Any<CancellationToken>()).Returns(call =>
            new ScreenplayPlanning().Plan(call.Arg<ScreenplayRenderRequest>(), call.Arg<CancellationToken>()));
        await File.WriteAllTextAsync(_document, RegisterProjectCorpus.LegacyV1.SourceForms.Single(form => form.Name == "single").Documents.Single().Text);
        await RenderFirst();
        var path = ArtifactPublicationStorage.ManifestPath(_destination);
        var manifest = JsonNode.Parse(await File.ReadAllTextAsync(path))!;
        manifest["schemaVersion"] = "1";
        foreach (var artifact in manifest["artifacts"]!.AsArray())
        {
            artifact!.AsObject().Remove("sources");
        }
        await File.WriteAllTextAsync(path, manifest.ToJsonString());
        _before = Snapshot(_destination);
    }

    async Task Because() => _checked = await Capture();

    [Fact] void should_exit_with_changes_pending() => _checked.ExitCode.ShouldEqual(ExitCodes.ChangesPending);
    [Fact] void should_not_mutate_the_destination() => Snapshot(_destination).ShouldEqual(_before);

    [Fact]
    void should_list_resolved_sources_for_unchanged_artifacts()
    {
        using var output = JsonDocument.Parse(_checked.Stdout);
        var publication = output.RootElement.GetProperty("publication");
        publication.GetProperty("schemaVersion").GetString().ShouldEqual("2");
        var changes = publication.GetProperty("changes").EnumerateArray().ToArray();
        changes.ShouldNotBeEmpty();
        changes.All(change => change.GetProperty("kind").GetString() == "unchanged").ShouldBeTrue();
        var sources = changes.SelectMany(change => change.GetProperty("sources").EnumerateArray()).ToArray();
        sources.Any(source => source.GetProperty("kind").GetString() == "command" && source.GetProperty("address").GetString() == "Projects/Registration/RegisterProject/RegisterProject").ShouldBeTrue();
        publication.GetProperty("bySource").GetArrayLength().ShouldBeGreaterThan(0);
    }
}
