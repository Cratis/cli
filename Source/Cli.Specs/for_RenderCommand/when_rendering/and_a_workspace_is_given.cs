// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;
using Cratis.Cli.for_ScreenplayPlanning;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_a_workspace_is_given : given.a_workspace_render_command
{
    [Theory]
    [InlineData("single", null, null, false)]
    [InlineData("folder", null, null, true)]
    [InlineData("single", "Delivery.Backend", "Company.Projects", true)]
    [InlineData("folder", "Delivery.Backend", "Company.Projects", false)]
    [InlineData("single", "Delivery.Backend", null, false)]
    [InlineData("folder", null, "Company.Projects", false)]
    public async Task should_publish_the_frozen_corpus_without_replacing_imported_identities(string form, string? projectName, string? rootNamespace, bool supplyName)
    {
        var workspace = CreateWorkspace(form);
        WriteWorkspace(workspace);
        var originalBytes = await File.ReadAllBytesAsync(_input);
        _settings.Name = supplyName ? workspace.ApplicationName : null;
        _settings.ProjectName = projectName;
        _settings.RootNamespace = rootNamespace;
        _settings.Target = "CRATIS";
        _settings.Force = true;
        var model = SemanticModelSerializer.Deserialize(Corpus.EsmBytes.AsSpan());
        var execution = SemanticExecutionPlan.Compile(model);
        var options = new CratisRenderingOptions(projectName ?? Corpus.ApplicationName, rootNamespace ?? Corpus.ApplicationName);
        var expected = CratisRendering.Plan(model, execution.Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), options);
        expected.Success.ShouldBeTrue();

        (await Execute()).ShouldEqual(ExitCodes.Success);

        var compiled = _documentSets.Single();
        compiled.Documents.Length.ShouldEqual(workspace.Documents.Length);
        SemanticIdentityCatalogSerializer.Serialize(compiled.IdentityCatalog).SequenceEqual(SemanticIdentityCatalogSerializer.Serialize(workspace.IdentityCatalog)).ShouldBeTrue();
        foreach (var document in workspace.Documents)
        {
            document.Id.ShouldNotEqual(DocumentId.Create(document.StableKey));
            var actual = compiled.Documents.Single(actual => actual.Id == document.Id);
            actual.StableKey.ShouldEqual(document.StableKey);
            actual.DisplayPath.ShouldEqual(document.Path.Value);
            actual.Text.ShouldEqual(document.Text);
        }

        SemanticModelSerializer.Serialize(_requests.Single().Model).SequenceEqual(Corpus.EsmBytes).ShouldBeTrue();
        when_planning_canonical_source_forms.AssertProfile(_requests.Single().Profile, CratisRendering.CreateProfile(Corpus.ApplicationName, options));
        await _publication.Received(1).Publish(Arg.Is<ArtifactPublicationRequest>(request => request.Force && request.Destination == _destination), Arg.Any<CancellationToken>());
        foreach (var artifact in expected.Artifacts)
        {
            (await File.ReadAllBytesAsync(Path.Combine(_destination, artifact.RelativePath))).SequenceEqual(artifact.Bytes).ShouldBeTrue();
        }

        (await File.ReadAllBytesAsync(_input)).SequenceEqual(originalBytes).ShouldBeTrue();
        File.Exists(Path.Combine(_destination, ".cratis-render.json")).ShouldBeTrue();
        var manifest = await File.ReadAllBytesAsync(Path.Combine(_destination, ".cratis-render.json"));
        (await Execute()).ShouldEqual(ExitCodes.Success);
        (await File.ReadAllBytesAsync(Path.Combine(_destination, ".cratis-render.json"))).SequenceEqual(manifest).ShouldBeTrue();
    }

    [Theory]
    [InlineData("single")]
    [InlineData("folder")]
    public async Task should_preserve_an_application_identity_independent_of_its_name(string form)
    {
        var workspace = CreateWorkspace(form, externalIdentity: true);
        workspace.Compilation.Success.ShouldBeTrue();
        workspace.IdentityCatalog.Application.ShouldNotEqual(ApplicationIdentity.Create(workspace.ApplicationName));
        WriteWorkspace(workspace);
        _settings.Name = workspace.ApplicationName;

        (await Execute()).ShouldEqual(ExitCodes.Success);

        var model = _requests.Single().Model;
        model.Application.Id.ShouldEqual(SemanticId.Create(SemanticKind.Application, "persisted-application-node-42"));
        SemanticModelSerializer.Serialize(model).SequenceEqual(SemanticModelSerializer.Serialize(workspace.Compilation.Value!.Model)).ShouldBeTrue();
        SemanticIdentityCatalogSerializer.Serialize(_documentSets.Single().IdentityCatalog).SequenceEqual(SemanticIdentityCatalogSerializer.Serialize(workspace.IdentityCatalog)).ShouldBeTrue();
    }

    [Theory]
    [InlineData("single", false)]
    [InlineData("folder", true)]
    public async Task should_accept_the_workspace_option_through_the_normal_command_parser(string form, bool supplyName)
    {
        var workspace = CreateWorkspace(form);
        WriteWorkspace(workspace);
        string[] arguments = ["render", "--workspace", _input, "--destination", _destination, "--target", "cratis", "--project-name", "Delivery.Backend", "--root-namespace", Corpus.ApplicationName, "--force", "-o", "json-compact"];
        if (supplyName)
        {
            arguments = [.. arguments, "--name", workspace.ApplicationName];
        }

        (await CliApp.Create().RunAsync(arguments)).ShouldEqual(ExitCodes.Success);

        var model = SemanticModelSerializer.Deserialize(Corpus.EsmBytes.AsSpan());
        var expected = CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Delivery.Backend", Corpus.ApplicationName));
        expected.Success.ShouldBeTrue();
        foreach (var artifact in expected.Artifacts)
        {
            (await File.ReadAllBytesAsync(Path.Combine(_destination, artifact.RelativePath))).SequenceEqual(artifact.Bytes).ShouldBeTrue();
        }
    }
}
