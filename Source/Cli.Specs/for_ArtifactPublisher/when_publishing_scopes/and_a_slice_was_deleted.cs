// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher.when_publishing_scopes;

public class and_a_slice_was_deleted : for_ScreenplayPlanning.when_planning_scopes.given.a_scoped_model
{
    ArtifactPublicationResult _result = null!;
    ArtifactPublicationCheckResult _check = null!;
    byte[] _other = null!;
    string _otherPath = null!;

    async Task Establish()
    {
        await Publish(await Plan("Projects.Registration", "Tasks"));
        _otherPath = Path.Combine(_destination, "Tasks/Backlog/Intake/CaptureTask/CaptureTask.cs");
        _other = File.ReadAllBytes(_otherPath);
        File.Delete(_lookupFile);
    }

    async Task Because()
    {
        var plan = await Plan("Projects.Registration");
        _check = await _publisher.Check(Request(plan), CancellationToken.None);
        _result = await Publish(plan);
    }

    [Fact] void should_remove_only_the_deleted_slice() => _result.Removed.ShouldEqual(1);
    [Fact] void should_preserve_other_scopes() => File.ReadAllBytes(_otherPath).SequenceEqual(_other).ShouldBeTrue();
    [Fact] void should_agree_with_the_check() => _check.Removed.ShouldEqual(_result.Removed);
    [Fact] void should_keep_other_scopes_managed() => ArtifactPublicationStorage.ReadManifest(_destination)!.Artifacts.Any(artifact => artifact.Path.StartsWith("Tasks/", StringComparison.Ordinal)).ShouldBeTrue();
}
