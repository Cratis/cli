// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_semantic_sources_are_receipted : given.a_workspace_render_command
{
    JsonDocument _output = null!;
    ArtifactManifest _manifest = null!;

    void Establish() => WriteWorkspace(CreateWorkspace());

    async Task Because()
    {
        var previous = Console.Out;
        await using var output = new StringWriter();
        try
        {
            Console.SetOut(output);
            (await Execute()).ShouldEqual(ExitCodes.Success);
            _output = JsonDocument.Parse(output.ToString());
            _manifest = ArtifactPublicationStorage.ReadManifest(_destination)!;
        }
        finally
        {
            Console.SetOut(previous);
        }
    }

    [Fact] void should_report_schema_two() => Receipt.GetProperty("schemaVersion").GetString().ShouldEqual("2");
    [Fact] void should_receipt_the_command() => AssertDeclaration("command", "Projects/Registration/RegisterProject/RegisterProject", "Projects/Registration/RegisterProject/RegisterProject.cs");
    [Fact] void should_receipt_the_event() => AssertDeclaration("event", "Projects/Registration/RegisterProject/ProjectRegistered", "Projects/Registration/RegisterProject/RegisterProject.cs");
    [Fact] void should_receipt_the_read_model() => AssertDeclaration("readmodel", "Projects/Registration/ProjectLookup/ProjectSummary", "Projects/Registration/ProjectLookup/ProjectLookup.cs");
    [Fact] void should_receipt_the_specification() => AssertDeclaration("specification", "Projects/Registration/RegisterProject/RegisteringAProject", "Projects/Registration/RegisterProject/when_registering_aproject.cs");
    [Fact] void should_leave_scaffold_sources_empty() => Changes.Single(change => change.GetProperty("path").GetString() == "Projects.csproj").GetProperty("sources").GetArrayLength().ShouldEqual(0);
    [Fact] void should_store_schema_two() => _manifest.SchemaVersion.ShouldEqual("2");

    [Fact]
    void should_store_every_planned_source_in_order()
    {
        foreach (var change in Changes)
        {
            var path = change.GetProperty("path").GetString()!;
            _manifest.Artifacts.Single(artifact => artifact.Path == path).Sources.ShouldEqual(change.GetProperty("sources").EnumerateArray().Select(source => source.GetProperty("id").GetString()!));
            change.GetProperty("afterSha256").GetString().ShouldEqual(ArtifactPublicationStorage.Hash(Path.Combine(_destination, path)));
        }
    }

    [Fact]
    void should_group_all_sources_in_address_then_id_order_with_sorted_paths()
    {
        var entries = Receipt.GetProperty("bySource").EnumerateArray().ToArray();
        entries.Length.ShouldBeGreaterThan(0);
        var addresses = entries.Select(entry => entry.GetProperty("address").GetString()).ToArray();
        addresses.ShouldEqual(addresses.Order(StringComparer.Ordinal));
        foreach (var entry in entries)
        {
            var id = entry.GetProperty("id").GetString();
            var expected = Changes.Where(change => change.GetProperty("sources").EnumerateArray().Any(source => source.GetProperty("id").GetString() == id)).Select(change => change.GetProperty("path").GetString()).Order(StringComparer.Ordinal);
            entry.GetProperty("paths").EnumerateArray().Select(path => path.GetString()).ShouldEqual(expected);
        }
    }

    JsonElement Receipt => _output.RootElement.GetProperty("publication");
    JsonElement[] Changes => [.. Receipt.GetProperty("changes").EnumerateArray()];

    void AssertDeclaration(string kind, string address, string path)
    {
        var change = Changes.Single(change => change.GetProperty("path").GetString() == path);
        var source = change.GetProperty("sources").EnumerateArray().Single(source => source.GetProperty("kind").GetString() == kind);
        source.GetProperty("address").GetString().ShouldEqual(address);
        var id = source.GetProperty("id").GetString();
        var slices = _requests.Single().Model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).ToArray();
        var expectedId = kind switch
        {
            "command" => slices.SelectMany(slice => slice.Commands).Single().Id,
            "event" => slices.SelectMany(slice => slice.Events).Single().Id,
            "readmodel" => slices.SelectMany(slice => slice.ReadModels).Single().Id,
            _ => slices.SelectMany(slice => slice.Specifications).Single(specification => specification.Name == "RegisteringAProject").Id
        };
        id.ShouldEqual(expectedId.ToString());
        _manifest.Artifacts.Single(artifact => artifact.Path == path).Sources.ShouldContain(id);
        var grouped = Receipt.GetProperty("bySource").EnumerateArray().Single(entry => entry.GetProperty("id").GetString() == id);
        grouped.GetProperty("kind").GetString().ShouldEqual(kind);
        grouped.GetProperty("address").GetString().ShouldEqual(address);
        grouped.GetProperty("paths").EnumerateArray().Select(value => value.GetString()).ShouldContain(path);
    }

    void Destroy() => _output.Dispose();
}
