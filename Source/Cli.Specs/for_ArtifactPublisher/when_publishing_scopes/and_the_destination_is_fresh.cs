// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher.when_publishing_scopes;

public class and_the_destination_is_fresh : for_ScreenplayPlanning.when_planning_scopes.given.a_scoped_model
{
    ArtifactPublicationResult _result = null!;
    ArtifactPublicationCheckResult _check = null!;
    bool _checkWrote;

    async Task Because()
    {
        var plan = await Plan("Tasks.Backlog.Intake");
        _check = await _publisher.Check(Request(plan), CancellationToken.None);
        _checkWrote = Directory.Exists(_destination);
        _result = await Publish(plan);
    }

    [Fact] void should_write_the_application_scaffold() => File.Exists(Path.Combine(_destination, "Shop.csproj")).ShouldBeTrue();
    [Fact] void should_write_only_the_selected_slice() => Directory.Exists(Path.Combine(_destination, "Projects")).ShouldBeFalse();
    [Fact] void should_not_add_a_scene() => File.Exists(Path.Combine(_destination, "scene.json")).ShouldBeFalse();
    [Fact] void should_agree_with_check() => _check.Written.ShouldEqual(_result.Written);
    [Fact] void should_keep_check_read_only() => _checkWrote.ShouldBeFalse();
    [Fact] void should_record_the_resolved_scope() => _result.Receipt.Scope!.Single().ShouldEqual(new RenderScope("Tasks.Backlog.Intake", "feature"));
    [Fact] void should_record_outside_slices() => _result.Receipt.OutsideScope!.ShouldContain("Projects.Registration.RegisterProject");
    [Fact] void should_record_the_same_check_scope() => _check.Receipt.Scope!.Single().ShouldEqual(_result.Receipt.Scope!.Single());
}
