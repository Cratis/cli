// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayDiagnostics.when_heading_a_group;

public class and_the_group_holds_information : Specification
{
    string _result;

    void Because() => _result = ScreenplayDiagnosticsWriter.GroupHeadingFor(ScreenplayDiagnosticSeverity.Information);

    [Fact] void should_read_as_english_rather_than_a_pluralized_label() => _result.ShouldEqual("information");
}
