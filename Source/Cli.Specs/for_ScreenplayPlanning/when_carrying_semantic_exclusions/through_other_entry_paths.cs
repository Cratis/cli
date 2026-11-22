// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Cli.for_ScreenplayPlanning.when_carrying_semantic_exclusions;

public class through_other_entry_paths : given.an_excluded_slice
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_carry_real_exclusions_from_a_file_to_stage_evidence(bool strict)
    {
        var result = await _planning.Plan(new(_file, "Projects", "cratis") { Strict = strict }, CancellationToken.None);
        result.Success.ShouldBeTrue();
        var request = _requests.Single();
        var feature = request.Model.Application.Modules.Single().Features.Single();
        feature.Slices.Single().Name.ShouldEqual("RegisterProject");
        var exclusion = request.Exclusions.Single();
        exclusion.ParentId.ShouldEqual(feature.Id);
        result.Artifacts.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == ArtifactRenderDegradationCodes.UnsupportedConstruct && diagnostic.Artifact == feature.Id);
        AssertStageExclusions(result.Artifacts.Exclusions, request.Exclusions);
        var report = Text(result.Artifacts.Artifacts.Single(artifact => artifact.RelativePath == "UNSUPPORTED.md"));
        report.ShouldContain(exclusion.Address);
        report.ShouldContain(exclusion.Diagnostics[0].Code);
        result.Artifacts.Artifacts.Any(artifact => artifact.Sources.Contains(feature.Id) && Text(artifact).Contains("// Differs from Screenplay: Projects/Registration/Removed excluded (PLAY0268)", StringComparison.Ordinal)).ShouldBeTrue();
        if (strict)
        {
            var inventory = result.Artifacts.Inventory;
            inventory.ShouldNotBeNull();
            inventory.Constructs.ShouldNotBeEmpty();
            inventory.Constructs.Any(entry => entry.SemanticLocator.Parts.Any(part => part.Key == "Removed")).ShouldBeFalse();
        }
        else
        {
            // Stage 6.4.1 does not infer an inventory without the identity catalog supplied in strict mode.
            result.Artifacts.Inventory.ShouldBeNull();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_leave_healthy_or_precompiled_requests_empty_and_digest_neutral(bool precompiled)
    {
        var documents = Documents(HealthySource);
        var request = new ScreenplayDocumentRenderRequest(documents, "Projects", "cratis");
        var compiled = new SemanticModelCompiler().Compile("Projects", documents);
        var result = precompiled
            ? _planning.PlanCompiled(request, compiled, null, null, CancellationToken.None)
            : _planning.PlanDocuments(request, CancellationToken.None);
        result.Success.ShouldBeTrue();
        _requests.Single().Exclusions.IsDefault.ShouldBeFalse();
        _requests.Single().Exclusions.ShouldBeEmpty();
        var direct = _planning.PlanCompiled(request, compiled, null, null, CancellationToken.None);
        result.Artifacts.Digest.ShouldEqual(direct.Artifacts.Digest);
    }

    [Fact]
    public void should_support_a_compiler_without_admissible_compilation()
    {
        var documents = Documents(HealthySource);
        var ordinary = Substitute.For<ISemanticModelCompiler>();
        ordinary.Compile("Projects", documents).Returns(new SemanticModelCompiler().Compile("Projects", documents));
        var planning = CreatePlanning(ordinary);
        var result = planning.PlanDocuments(new(documents, "Projects", "cratis"), CancellationToken.None);
        result.Success.ShouldBeTrue();
        _requests.Single().Exclusions.IsDefault.ShouldBeFalse();
        _requests.Single().Exclusions.ShouldBeEmpty();
        ordinary.Received(1).Compile("Projects", documents);
    }

    [Fact]
    public void should_refuse_hard_source_errors_without_invoking_stage()
    {
        var result = _planning.PlanDocuments(new(Documents("not a screenplay declaration\n"), "Projects", "cratis"), CancellationToken.None);
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error);
        _requests.ShouldBeEmpty();
    }

    [Fact]
    public void should_filter_out_the_excluded_sibling_of_a_surviving_slice()
    {
        var result = _planning.PlanDocuments(new(_documents, "Projects", "cratis") { Scope = ["Projects.Registration.RegisterProject"] }, CancellationToken.None);
        result.Success.ShouldBeTrue();
        _requests.Single().Exclusions.ShouldBeEmpty();
    }
}
