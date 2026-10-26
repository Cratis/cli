// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_the_render_target_is_canceled : given.a_failing_render_target
{
    void Establish() => _failure = new OperationCanceledException();

    void Because() => _exception = Catch.Exception(() => _planning.PlanDocuments(_request, CancellationToken.None));

    [Fact] void should_preserve_the_cancellation() => _exception.ShouldBeSame(_failure);
}
