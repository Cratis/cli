// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Cli.Commands.Render.Publication;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Workspaces;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.for_RenderCommand.given;

public class a_workspace_render_command : Specification
{
    protected static readonly CanonicalCorpusVector Corpus = RegisterProjectCorpus.LegacyV1;
    protected readonly List<SemanticDocumentSet> _documentSets = [];
    protected readonly List<ArtifactRenderRequest> _requests = [];
    protected string _folder = null!;
    protected string _input = null!;
    protected string _destination = null!;
    protected RenderSettings _settings = null!;
    protected RenderCommand _command = null!;
    protected Action? _afterCompilation;
    private protected IArtifactPublication _publication = null!;

    void Establish()
    {
        _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())).FullName;
        _input = Path.Combine(_folder, "application.workspace.json");
        _destination = Path.Combine(_folder, "output");
        var compiler = Substitute.For<ISemanticModelCompiler>();
        compiler.Compile(Arg.Any<string>(), Arg.Any<SemanticDocumentSet>()).Returns(call =>
        {
            var documents = call.Arg<SemanticDocumentSet>();
            _documentSets.Add(documents);
            var result = new SemanticModelCompiler().Compile(call.Arg<string>(), documents);
            _afterCompilation?.Invoke();
            return result;
        });
        var planner = Substitute.For<IArtifactRenderPlanner>();
        planner.Plan(Arg.Any<ArtifactRenderRequest>()).Returns(call =>
        {
            var request = call.Arg<ArtifactRenderRequest>();
            _requests.Add(request);
            return new CratisArtifactRenderPlanner().Plan(request);
        });
        var publisher = new ArtifactPublisher();
        _publication = Substitute.For<IArtifactPublication>();
        _publication.Recover(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call =>
            publisher.Recover(call.Arg<string>(), call.Arg<CancellationToken>()));
        _publication.Publish(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>()).Returns(call =>
            publisher.Publish(call.Arg<ArtifactPublicationRequest>(), call.Arg<CancellationToken>()));
        _command = new(new ScreenplayPlanning(compiler, new RenderTargetRoster([new CratisRenderTarget(planner)])), _publication);
        _settings = new() { Workspace = _input, Destination = _destination, Output = OutputFormats.JsonCompact };
    }

    protected static ScreenplayWorkspace CreateWorkspace(string formName = "single", bool externalIdentity = false, bool invalidSource = false)
    {
        var form = Corpus.SourceForms.Single(form => form.Name == formName);
        var original = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var application = externalIdentity ? ApplicationIdentity.Create("persisted-studio-application-42") : original.Application;
        var documents = form.Documents.Select((document, index) => WorkspaceDocument.Create(
            DocumentId.Create($"persisted-document-{index}"),
            document.StableKey,
            PortablePlayPath.Parse($"imported/{document.DisplayPath}"),
            invalidSource
                ? Encoding.UTF8.GetBytes("this is not a screenplay\n")
                : [0xef, 0xbb, 0xbf, .. Encoding.UTF8.GetBytes(document.Text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\n", "\r\n", StringComparison.Ordinal))])).ToArray();
        var catalog = SemanticIdentityCatalog.Create(
            application,
            [.. documents.Select(document => new DocumentIdentityAssignment(document.StableKey, document.Id, SemanticIdentityOrigin.Persisted))],
            externalIdentity
                ? [new(SemanticAddress.ForApplication(application), SemanticId.Create(SemanticKind.Application, "persisted-application-node-42"), SemanticIdentityOrigin.Persisted)]
                : original.Semantics,
            externalIdentity ? [] : original.EventContracts);
        return ScreenplayWorkspace.Create(application, Corpus.ApplicationName, [.. documents], catalog);
    }

    protected void WriteWorkspace(ScreenplayWorkspace workspace) => File.WriteAllBytes(_input, ScreenplayWorkspaceSerializer.Serialize(workspace));

    protected Task<int> Execute(CancellationToken cancellationToken = default) =>
        ((ICommand<RenderSettings>)_command).ExecuteAsync(
            new CommandContext([], Substitute.For<IRemainingArguments>(), "render", null), _settings, cancellationToken);

    protected async Task AssertNoPublication(bool recovered = false)
    {
        await _publication.DidNotReceive().Publish(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>());
        if (!recovered)
        {
            await _publication.DidNotReceive().Recover(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        Directory.Exists(_destination).ShouldBeFalse();
    }

    void Destroy()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }
}
