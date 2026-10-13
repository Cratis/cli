// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher.when_checking_event_identities;

public class with_an_unchanged_application_id : given.an_existing_event_publication
{
    ArtifactPublicationResult _result = null!;

    void Establish() => File.WriteAllText(_projectFile,
        ProjectSource.Replace("A project was registered", "The project registration was recorded", StringComparison.Ordinal));

    async Task Because()
    {
        var plan = await Plan();
        _check = await _publisher.Check(Request(plan), CancellationToken.None);
        _result = await Publish(plan);
    }

    [Fact] void should_admit_the_read_only_check() => _check.Refused.ShouldEqual(0);
    [Fact] void should_publish_the_changed_artifacts() => _result.Written.ShouldBeGreaterThan(0);
    [Fact] void should_preserve_the_argument_free_event_id() => File.ReadAllText(Path.Combine(_destination, _eventPath)).ShouldContain("[global::Cratis.Chronicle.Events.EventTypeAttribute]\npublic record ProjectRegistered(");
    [Fact] void should_publish_the_new_documentation() => File.ReadAllText(Path.Combine(_destination, _eventPath)).ShouldContain("The project registration was recorded");
}
