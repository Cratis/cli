// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher.when_publishing_scopes;

public class and_a_dependency_is_already_managed : for_ScreenplayPlanning.when_planning_scopes.given.a_scoped_model
{
    ArtifactPublicationResult _result = null!;
    ArtifactPublicationCheckResult _check = null!;

    async Task Establish() => await Publish(await Plan("Projects.Registration.RegisterProject"));
    async Task Because()
    {
        var plan = await Plan("Projects.Registration.ProjectLookup");
        _check = await _publisher.Check(Request(plan), CancellationToken.None);
        _result = await Publish(plan);
    }

    [Fact] void should_accept_the_existing_dependency() => _check.Refused.ShouldEqual(0);
    [Fact] void should_write_the_selected_slice() => _result.Written.ShouldEqual(1);
    [Fact] void should_keep_the_dependency_managed() => ArtifactPublicationStorage.ReadManifest(_destination)!.Artifacts.Any(artifact => artifact.Path.EndsWith("/RegisterProject.cs", StringComparison.Ordinal)).ShouldBeTrue();
}
