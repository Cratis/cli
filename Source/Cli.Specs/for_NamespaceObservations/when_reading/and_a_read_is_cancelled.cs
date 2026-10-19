// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Cli.for_NamespaceObservations.when_reading;

public class and_a_read_is_cancelled : given.namespace_observations
{
    CancellationTokenSource _source = null!;

    void Establish()
    {
        _source = new CancellationTokenSource();
        ObserversThrow(StatusCode.Cancelled);
    }

    async Task Because()
    {
        await _source.CancelAsync();
        _failure = await Catch.Exception(() => _observations.Consumers("Library", "Default", _source.Token));
    }

    void Destroy() => _source.Dispose();

    [Fact] void should_report_cancellation() => (_failure is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_not_report_a_read_failure() => (_failure is ChronicleReadFailed).ShouldBeFalse();
}
