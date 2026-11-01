// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class with_released_strict_acceptance : given.a_classic_reducer_destination
{
    void Establish()
    {
        File.AppendAllText(_document, string.Join('\n',
            "",
            "module People",
            "  feature Directory",
            "    slice StateView Listings",
            "      readmodel PersonSummary",
            "        personId Uuid",
            "        name String",
            "      query FindPeople => PersonSummary[]",
            "        filter name String",
            "        performer",
            "          ```csharp",
            "          return missing;",
            "          ```",
            ""));
        _settings.Path = _folder;
        _settings.MigrateReducer = [ReadModel];
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_publish_artifacts_and_removals_before_returning_acceptance(bool strict)
    {
        _settings.Strict = strict;

        var output = await Capture();

        output.ExitCode.ShouldEqual(strict ? ExitCodes.StrictUnaccepted : ExitCodes.Success);
        var plan = _plans.Single().Artifacts!;
        plan.CanPublishArtifacts.ShouldBeTrue();
        plan.Success.ShouldEqual(!strict);
        File.Exists(ReducerFile).ShouldBeFalse();
        plan.RequiredRemovals.Single().RelativePath.ShouldEqual(_reducerPath);
        ArtifactPublicationStorage.ReadManifest(_destination)!.Artifacts.Select(artifact => artifact.Path).ShouldNotContain(_reducerPath);
        foreach (var artifact in plan.Artifacts)
        {
            (await File.ReadAllBytesAsync(Path.Combine(_destination, artifact.RelativePath), TestContext.Current.CancellationToken)).ShouldEqual<byte>([.. artifact.Bytes]);
        }
        ArtifactPublicationStorage.HasPendingRecovery(_destination).ShouldBeFalse();
        if (strict) await AssertCompletenessReports(plan);
    }

    async Task AssertCompletenessReports(ArtifactRenderPlan plan)
    {
        plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Degraded);
        plan.Degradations.ShouldNotBeEmpty();
        foreach (var path in new[] { ".stage/completeness.json", "UNSUPPORTED.md" })
        {
            var report = plan.Artifacts.Single(artifact => artifact.RelativePath == path);
            (await File.ReadAllBytesAsync(Path.Combine(_destination, report.RelativePath), TestContext.Current.CancellationToken)).ShouldEqual<byte>([.. report.Bytes]);
        }
    }
}
