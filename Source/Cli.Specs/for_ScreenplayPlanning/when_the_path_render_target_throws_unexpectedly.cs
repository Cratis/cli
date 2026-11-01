// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_the_path_render_target_throws_unexpectedly : given.a_failing_render_target
{
    async Task Because() => _exception = await Catch.Exception(() => _planning.Plan(
        new ScreenplayRenderRequest(_file, "Projects", "failing", null, null), CancellationToken.None));

    [Fact] void should_wrap_the_failure() => (_exception is RenderFailedUnexpectedly).ShouldBeTrue();
    [Fact] void should_preserve_the_inner_exception() => _exception!.InnerException.ShouldBeSame(_failure);
    [Fact] void should_identify_the_diagnostic() => _exception!.Message.ShouldContain("CLI-RENDER-027");
}
