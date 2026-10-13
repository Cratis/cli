// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher.when_checking_event_identities;

public class with_a_pinned_rename : given.an_existing_event_publication
{
    ArtifactPublicationResult _result = null!;

    void Establish()
    {
        PinEvent("ProjectRegistered");
        File.WriteAllText(_projectFile, File.ReadAllText(_projectFile)
            .Replace("ProjectRegistered", "ProjectRegistrationRecorded", StringComparison.Ordinal)
            .Replace("id \"ProjectRegistrationRecorded\"", "id \"ProjectRegistered\"", StringComparison.Ordinal));
        File.WriteAllText(_lookupFile, LookupSource.Replace("ProjectRegistered", "ProjectRegistrationRecorded", StringComparison.Ordinal));
    }

    async Task Because()
    {
        var plan = await Plan();
        _check = await _publisher.Check(Request(plan), CancellationToken.None);
        _result = await Publish(plan);
    }

    [Fact] void should_admit_the_read_only_check() => _check.Refused.ShouldEqual(0);
    [Fact] void should_publish_the_renamed_event() => _result.Written.ShouldBeGreaterThan(0);
    [Fact] void should_keep_the_stored_id() => File.ReadAllText(Path.Combine(_destination, _eventPath)).ShouldContain("[global::Cratis.Chronicle.Events.EventTypeAttribute(\"ProjectRegistered\")]\npublic record ProjectRegistrationRecorded(");
}
