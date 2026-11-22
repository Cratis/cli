// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_mapping_source_lines;

public class without_a_known_layout : given.an_inline_model
{
    (bool Success, string[] Code) _withoutDestination;
    (bool Success, string[] Code) _withoutModelRoot;

    void Because()
    {
        _withoutDestination = Plan(null, _root);
        _withoutModelRoot = Plan(_folder, null);
    }

    [Fact] void should_keep_mapping_off_without_a_destination() => AssertMappingOff(_withoutDestination);
    [Fact] void should_keep_mapping_off_without_a_model_root() => AssertMappingOff(_withoutModelRoot);

    static void AssertMappingOff((bool Success, string[] Code) result)
    {
        result.Success.ShouldBeTrue();
        result.Code.ShouldContain(code => code.Contains(Body, StringComparison.Ordinal));
        result.Code.ShouldNotContain(code => code.Contains("#line ", StringComparison.Ordinal));
    }

    (bool Success, string[] Code) Plan(string? destination, string? modelRoot)
    {
        _result = _planning.PlanDocuments(DocumentRequest(destination, modelRoot), CancellationToken.None);
        return (_result.Success, RenderedCode());
    }
}
