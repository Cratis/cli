// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.for_ScreenplayPlanning.given;

public class a_canonical_screenplay : Specification
{
    protected static readonly CanonicalCorpusVector Corpus = RegisterProjectCorpus.LegacyV1;
    protected readonly List<ArtifactRenderRequest> _requests = [];
    protected readonly List<SemanticDocumentSet> _documentSets = [];
    private protected ScreenplayPlanning _planning = null!;
    string _folder = null!;

    void Establish()
    {
        _folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var planner = Substitute.For<IArtifactRenderPlanner>();
        planner.Plan(Arg.Any<ArtifactRenderRequest>()).Returns(call =>
        {
            var request = call.Arg<ArtifactRenderRequest>();
            _requests.Add(request);
            return new CratisArtifactRenderPlanner().Plan(request);
        });
        var compiler = Substitute.For<ISemanticModelCompiler>();
        compiler.Compile(Arg.Any<string>(), Arg.Any<SemanticDocumentSet>()).Returns(call =>
        {
            var documents = call.Arg<SemanticDocumentSet>();
            _documentSets.Add(documents);
            return new SemanticModelCompiler().Compile(call.Arg<string>(), documents);
        });
        _planning = new(compiler, new RenderTargetRoster([new CratisRenderTarget(planner)]));
    }

    protected string WriteSource(string variant)
    {
        var form = Corpus.SourceForms.Single(_ => _.Name == (variant == "single" ? "single" : "folder"));
        var root = variant == "relocated" ? Path.Combine(_folder, "another", "checkout", "plays") : _folder;
        CanonicalCorpusDocument[] documents = variant == "reordered" ? [.. form.Documents.Reverse()] : [.. form.Documents];
        for (var index = 0; index < documents.Length; index++)
        {
            var document = documents[index];

            // Renaming changes the CLI's ordinal compilation order, not just file creation order.
            var relativePath = variant == "reordered" ? $"{index:D2}.play" : document.DisplayPath;
            var path = Path.Combine(root, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, document.Bytes.AsSpan());
        }

        return variant == "single" ? Path.Combine(root, documents.Single().DisplayPath) : root;
    }

    void Destroy()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }
}
