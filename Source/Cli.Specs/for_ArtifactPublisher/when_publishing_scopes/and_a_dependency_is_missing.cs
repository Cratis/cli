// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher.when_publishing_scopes;

public class and_a_dependency_is_missing : for_ScreenplayPlanning.when_planning_scopes.given.a_scoped_model
{
    ArtifactPublicationCheckResult _check = null!;
    Exception? _error;

    async Task Because()
    {
        var plan = await Plan("Projects.Registration.ProjectLookup");
        _check = await _publisher.Check(Request(plan), CancellationToken.None);
        _error = await Catch.Exception(() => Publish(plan));
    }

    [Fact] void should_refuse_the_render() => _error.ShouldBeOfExactType<UnsafeArtifactPublication>();
    [Fact] void should_report_the_typed_diagnostic() => _check.Receipt.Changes.Single().Code.ShouldEqual("CLI-RENDER-014");
    [Fact] void should_name_the_missing_event() => _check.Receipt.Changes.Single().Reason.ShouldContain("Projects.Registration.RegisterProject.ProjectRegistered");
    [Fact] void should_suggest_the_owning_scope() => _check.Receipt.Changes.Single().Reason.ShouldContain("--scope Projects.Registration.RegisterProject");
    [Fact] void should_write_nothing() => Directory.Exists(_destination).ShouldBeFalse();
}
