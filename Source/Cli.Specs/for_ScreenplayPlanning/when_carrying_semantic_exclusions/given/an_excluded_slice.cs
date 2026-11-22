// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.for_ScreenplayPlanning.when_carrying_semantic_exclusions.given;

public class an_excluded_slice : Specification
{
    internal const string HealthySource = "module Projects\n  feature Registration\n    slice StateChange RegisterProject\n      command RegisterProject\n        projectId Uuid identifier\n        name String\n        produces ProjectRegistered\n          for projectId\n          name = name\n      event ProjectRegistered\n        name String\n";
    internal const string RemovedSource = "    slice StateView Removed\n      readmodel RemovedView\n        id String\n      query Scoped => RemovedView[]\n        scoped to identity\n";
    internal const string Source = HealthySource + RemovedSource;

    protected string _folder = null!;
    protected string _file = null!;
    protected readonly List<ArtifactRenderRequest> _requests = [];
    protected SemanticDocumentSet _documents = null!;
    protected AdmissibleCompilationResult _expected = null!;
    private protected ScreenplayPlanning _planning = null!;

    void Establish()
    {
        _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"cli-exclusions-{Guid.NewGuid():N}")).FullName;
        _file = Path.Combine(_folder, "model.play");
        File.WriteAllText(_file, Source);
        _documents = Documents(Source);
        _expected = new SemanticModelCompiler().CompileAdmissible("Projects", _documents);
        _expected.Compilation.Success.ShouldBeTrue();
        _expected.Exclusions.ShouldNotBeEmpty();
        _planning = CreatePlanning(new SemanticModelCompiler());
    }

    private protected ScreenplayPlanning CreatePlanning(ISemanticModelCompiler compiler)
    {
        var planner = Substitute.For<IArtifactRenderPlanner>();
        planner.Plan(Arg.Any<ArtifactRenderRequest>()).Returns(call =>
        {
            var request = call.Arg<ArtifactRenderRequest>();
            _requests.Add(request);
            return new CratisArtifactRenderPlanner().Plan(request);
        });
        return new(compiler, new RenderTargetRoster([new CratisRenderTarget(planner)]));
    }

    internal static SemanticDocumentSet Documents(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "model.play", source);
        return SemanticDocumentSet.Create([document], catalog);
    }

    internal static void AssertExclusions(IEnumerable<SemanticExclusion> actual, IEnumerable<SemanticExclusion> expected)
    {
        actual.Count().ShouldEqual(expected.Count());
        foreach (var (entry, original) in actual.Zip(expected))
        {
            entry.Kind.ShouldEqual(original.Kind);
            entry.Address.ShouldEqual(original.Address);
            entry.ParentId.ShouldEqual(original.ParentId);
            entry.Location.ShouldEqual(original.Location);
            entry.Reason.ShouldEqual(original.Reason);
            entry.Cause.ShouldEqual(original.Cause);
            JsonSerializer.Serialize(entry.Diagnostics).ShouldEqual(JsonSerializer.Serialize(original.Diagnostics));
            entry.DependsOn.ShouldEqual<SemanticExclusionReference>(original.DependsOn);
        }
    }

    internal static void AssertStageExclusions(IEnumerable<SemanticExclusion> actual, IEnumerable<SemanticExclusion> expected) =>
        AssertExclusions(actual, expected.OrderBy(entry => entry.Kind.ToString(), StringComparer.Ordinal)
            .ThenBy(entry => entry.Address, StringComparer.Ordinal).Select(entry => entry with
            {
                Diagnostics = [.. entry.Diagnostics.OrderBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
                    .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
                    .ThenBy(diagnostic => JsonSerializer.Serialize(diagnostic), StringComparer.Ordinal)],
                DependsOn = [.. entry.DependsOn.OrderBy(dependency => dependency.Kind.ToString(), StringComparer.Ordinal)
                    .ThenBy(dependency => dependency.Address, StringComparer.Ordinal)]
            }));

    internal static string Text(PlannedArtifact artifact) => Encoding.UTF8.GetString(artifact.Bytes.AsSpan());

    void Destroy() => Directory.Delete(_folder, true);
}
