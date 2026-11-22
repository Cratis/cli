// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_mapping_source_lines;

public class without_reachable_sources : given.an_inline_model
{
    void Because()
    {
        File.Delete(_file);
        _result = _planning.PlanDocuments(DocumentRequest(Path.Combine(_folder, "output"), _root), CancellationToken.None);
    }

    [Fact] void should_still_plan_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_preserve_the_body() => RenderedCode().ShouldContain(code => code.Contains(Body, StringComparison.Ordinal));
    [Fact] void should_leave_mapping_off() => RenderedCode().ShouldNotContain(code => code.Contains("#line ", StringComparison.Ordinal));
}
