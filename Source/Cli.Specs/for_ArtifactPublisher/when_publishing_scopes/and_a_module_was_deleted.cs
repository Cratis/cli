// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher.when_publishing_scopes;

public class and_a_module_was_deleted : for_ScreenplayPlanning.when_planning_scopes.given.a_scoped_model
{
    ArtifactPublicationResult _result = null!;
    ArtifactPublicationCheckResult _check = null!;
    byte[] _task = null!;
    const string TaskPath = "Tasks/Backlog/Intake/CaptureTask/CaptureTask.cs";

    async Task Establish()
    {
        await Publish(await Plan("Projects", "Tasks"));
        _task = await File.ReadAllBytesAsync(Path.Combine(_destination, TaskPath));
        File.Delete(_taskFile);
    }

    async Task Because()
    {
        var plan = await Plan("Projects");
        _check = await _publisher.Check(Request(plan), CancellationToken.None);
        _result = await Publish(plan);
    }

    [Fact] void should_not_delete_unrelated_orphans() => _result.Removed.ShouldEqual(0);
    [Fact] void should_preserve_the_orphan_bytes() => File.ReadAllBytes(Path.Combine(_destination, TaskPath)).SequenceEqual(_task).ShouldBeTrue();
    [Fact] void should_keep_the_orphan_managed() => ArtifactPublicationStorage.ReadManifest(_destination)!.Artifacts.Select(artifact => artifact.Path).ShouldContain(TaskPath);
    [Fact] void should_report_the_orphan_in_publication() => _result.Receipt.Orphaned!.ShouldContain(TaskPath);
    [Fact] void should_report_the_orphan_in_check() => _check.Receipt.Orphaned!.ShouldContain(TaskPath);
}
