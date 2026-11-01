// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher.when_checking_event_identities;

public class with_a_directory_at_an_event_path : given.an_existing_event_publication
{
    string _eventFile = null!;

    void Establish()
    {
        _eventFile = Path.Combine(_destination, _eventPath);
        File.Delete(_eventFile);
        Directory.CreateDirectory(_eventFile);
    }

    async Task Because()
    {
        var plan = await Plan();
        _error = await Catch.Exception(() => _publisher.Publish(Request(plan) with { Force = true }, CancellationToken.None));
    }

    [Fact] void should_refuse_publication() => _error.ShouldBeOfExactType<UnsafeArtifactPublication>();
    [Fact] void should_explain_that_the_existing_file_could_not_be_read() => _error!.Message.ShouldContain("could not be read for the publication check");
    [Fact] void should_leave_the_directory_in_place() => Directory.Exists(_eventFile).ShouldBeTrue();
}
