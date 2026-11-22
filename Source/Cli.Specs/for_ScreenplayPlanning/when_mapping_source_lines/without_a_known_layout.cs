// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_mapping_source_lines;

public class without_a_known_layout : given.an_inline_model
{
    [Fact] void should_keep_mapping_off_without_a_destination() => AssertMappingOff(null, _root);
    [Fact] void should_keep_mapping_off_without_a_model_root() => AssertMappingOff(_folder, null);

    void AssertMappingOff(string? destination, string? modelRoot)
    {
        _result = _planning.PlanDocuments(DocumentRequest(destination, modelRoot), CancellationToken.None);

        _result.Success.ShouldBeTrue();
        RenderedCode().ShouldContain(code => code.Contains(Body, StringComparison.Ordinal));
        RenderedCode().ShouldNotContain(code => code.Contains("#line ", StringComparison.Ordinal));
    }
}
