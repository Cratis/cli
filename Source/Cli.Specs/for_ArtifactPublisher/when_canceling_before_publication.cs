// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ArtifactPublisher;

public class when_canceling_before_publication : given.an_artifact_publication
{
    Exception _error = null!;

    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        _error = await Catch.Exception(() => Publish(cancellationToken: cancellation.Token));
    }

    [Fact] void should_cancel() => _error.ShouldBeOfExactType<OperationCanceledException>();
    [Fact] void should_leave_the_destination_absent() => Directory.Exists(_destination).ShouldBeFalse();
}
