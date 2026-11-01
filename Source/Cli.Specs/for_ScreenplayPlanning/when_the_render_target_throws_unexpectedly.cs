// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_the_render_target_throws_unexpectedly : given.a_failing_render_target
{
    void Because() => _exception = Catch.Exception(() => _planning.PlanDocuments(_request, CancellationToken.None));

    [Fact] void should_wrap_the_failure() => _exception!.GetType().Name.ShouldEqual("RenderFailedUnexpectedly");
    [Fact] void should_preserve_the_inner_exception() => _exception!.InnerException.ShouldBeSame(_failure);
    [Fact] void should_identify_the_diagnostic() => _exception!.Message.ShouldContain("CLI-RENDER-027");
    [Fact] void should_identify_the_phase() => _exception!.Message.ShouldContain("planning");
    [Fact] void should_identify_the_exception_type() => _exception!.Message.ShouldContain("System.ArgumentException");
}
