// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayValidation.when_validating;

public class and_a_nested_document_has_an_error : given.a_folder_with_documents
{
    ValidatedScreenplay _result;

    void Establish()
    {
        WriteDocument("Concepts.play", ConceptsSource);
        WriteDocument("nested/Broken.play", InvalidSource);
    }

    void Because() => _result = _validation.Validate(_folder);

    [Fact] void should_count_the_rejected_document() => _result.FileCount.ShouldEqual(2);
    [Fact] void should_report_the_original_code() => _result.Diagnostics.Single().Code.ShouldEqual("PLAY0027");
    [Fact] void should_report_the_original_severity() => _result.Diagnostics.Single().Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Error);
    [Fact] void should_report_the_originating_file_and_position() => _result.Diagnostics.Single().Location.ShouldEqual("nested/Broken.play(5,5)");
    [Fact] void should_preserve_the_compiler_message() => _result.Diagnostics.Single().Message.ShouldEqual("Invalid slice declaration 'slice Reserving' - expected 'slice <Type> <Name>'");
}
