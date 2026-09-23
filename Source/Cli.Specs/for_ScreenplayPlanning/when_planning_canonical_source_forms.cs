// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text.Json;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_planning_canonical_source_forms : given.a_canonical_screenplay
{
    [Theory]
    [InlineData("single", null, null)]
    [InlineData("folder", null, null)]
    [InlineData("reordered", null, null)]
    [InlineData("relocated", null, null)]
    [InlineData("single", "Delivery.Backend", "Company.Projects")]
    [InlineData("folder", "Delivery.Backend", "Company.Projects")]
    [InlineData("reordered", "Delivery.Backend", "Company.Projects")]
    [InlineData("relocated", "Delivery.Backend", "Company.Projects")]
    [InlineData("single", "Delivery.Backend", null)]
    [InlineData("single", null, "Company.Projects")]
    public async Task should_compile_and_plan_the_exact_frozen_semantics_and_published_artifacts(string variant, string? projectName, string? rootNamespace)
    {
        // The oracle is the published corpus's frozen ESM, never the CLI compilation's output.
        // This is CLI consumer conformance, not a claim that the Studio host was executed.
        var expectedModel = SemanticModelSerializer.Deserialize(Corpus.EsmBytes.AsSpan());
        var expectedExecution = SemanticExecutionPlan.Compile(expectedModel);
        expectedExecution.Success.ShouldBeTrue();
        var scope = new ArtifactRenderScope(ArtifactRenderScopeKind.Application, expectedModel.Application.Id);
        var options = new CratisRenderingOptions(projectName ?? Corpus.ApplicationName, rootNamespace ?? Corpus.ApplicationName);
        var expectedProfile = CratisRendering.CreateProfile(Corpus.ApplicationName, options);
        var expected = CratisRendering.Plan(expectedModel, expectedExecution.Plan!, scope, options);
        expected.Success.ShouldBeTrue();
        expected.Artifacts.ShouldNotBeEmpty();
        var source = WriteSource(variant);

        var result = await _planning.Plan(new(source, Corpus.ApplicationName, CratisRendering.TargetId, projectName, rootNamespace), CancellationToken.None);

        result.Success.ShouldBeTrue();
        result.Documents.ShouldEqual(variant == "single" ? 1 : 5);
        result.Diagnostics.ShouldBeEmpty();
        _requests.Count.ShouldEqual(1);
        var request = _requests.Single();
        request.Scope.ShouldEqual(scope);
        request.Model.Application.Id.ShouldEqual(expectedModel.Application.Id);
        request.Model.Revision.ShouldEqual(Corpus.SemanticRevision);
        request.ExecutionPlan.Revision.ShouldEqual(Corpus.SemanticRevision);

        // Full canonical bytes pin every declaration, property, event contract and specification identity.
        SemanticModelSerializer.Serialize(request.Model).SequenceEqual(Corpus.EsmBytes).ShouldBeTrue();
        AssertProfile(request.Profile, expectedProfile);
        AssertPlan(result.Artifacts!, expected);
    }

    internal static void AssertProfile(ArtifactRenderProfile actual, ArtifactRenderProfile expected)
    {
        actual.Target.ShouldEqual(expected.Target);
        actual.TargetVersion.ShouldEqual(expected.TargetVersion);
        actual.Renderer.ShouldEqual(expected.Renderer);
        actual.RendererVersion.ShouldEqual(expected.RendererVersion);
        actual.Inputs.Select(_ => (_.Name, _.Version, _.Sha256)).ShouldEqual(expected.Inputs.Select(_ => (_.Name, _.Version, _.Sha256)));
        foreach (var (input, expectedInput) in actual.Inputs.Zip(expected.Inputs))
        {
            input.Bytes.SequenceEqual(expectedInput.Bytes).ShouldBeTrue();
            Hash(input.Bytes.AsSpan()).ShouldEqual(input.Sha256);
        }

        ProfileDigest(actual).ShouldEqual(ProfileDigest(expected));
    }

    internal static void AssertPlan(ArtifactRenderPlan actual, ArtifactRenderPlan expected)
    {
        actual.SchemaVersion.ShouldEqual(expected.SchemaVersion);
        actual.Target.ShouldEqual(expected.Target);
        actual.TargetVersion.ShouldEqual(expected.TargetVersion);
        actual.Renderer.ShouldEqual(expected.Renderer);
        actual.RendererVersion.ShouldEqual(expected.RendererVersion);
        actual.ApplicationName.ShouldEqual(Corpus.ApplicationName);
        actual.SemanticRevision.ShouldEqual(Corpus.SemanticRevision);
        Assert.Equal(expected.Artifacts.Select(_ => (_.Kind, _.RelativePath, _.Sha256)).ToArray(), actual.Artifacts.Select(_ => (_.Kind, _.RelativePath, _.Sha256)).ToArray());
        foreach (var (artifact, expectedArtifact) in actual.Artifacts.Zip(expected.Artifacts))
        {
            artifact.Bytes.SequenceEqual(expectedArtifact.Bytes).ShouldBeTrue();
            Hash(artifact.Bytes.AsSpan()).ShouldEqual(artifact.Sha256);
        }

        actual.Diagnostics.ShouldEqual(expected.Diagnostics);
        PlanDigest(actual).ShouldEqual(PlanDigest(expected));
    }

    /// <summary>
    /// Computes test-only aggregate evidence with explicit field and array order, not a cross-host wire contract.
    /// </summary>
    /// <param name="profile">The package-owned rendering profile.</param>
    /// <returns>The digest used only for conformance assertions.</returns>
    static string ProfileDigest(ArtifactRenderProfile profile) => Hash(JsonSerializer.SerializeToUtf8Bytes(new
    {
        profile.Target,
        profile.TargetVersion,
        profile.Renderer,
        profile.RendererVersion,
        Inputs = profile.Inputs.Select(_ => new { _.Name, _.Version, _.Sha256, Bytes = Convert.ToBase64String(_.Bytes.AsSpan()) }).ToArray()
    }));

    static string PlanDigest(ArtifactRenderPlan plan) => Hash(JsonSerializer.SerializeToUtf8Bytes(new
    {
        plan.SchemaVersion,
        plan.Target,
        plan.TargetVersion,
        plan.Renderer,
        plan.RendererVersion,
        plan.ApplicationName,
        SemanticRevision = plan.SemanticRevision.ToString(),
        Artifacts = plan.Artifacts.Select(_ => new { _.Kind, _.RelativePath, _.Sha256, Bytes = Convert.ToBase64String(_.Bytes.AsSpan()) }).ToArray(),
        Diagnostics = plan.Diagnostics.Select(_ => new { _.Code, _.Severity, _.Message, Artifact = _.Artifact.ToString() }).ToArray()
    }));

    static string Hash(ReadOnlySpan<byte> bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
}
