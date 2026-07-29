// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayValidation.when_validating;

public class and_the_document_has_an_error : given.a_folder_with_documents
{
    string _document;
    ValidatedScreenplay _result;

    void Establish() => _document = WriteDocument("MyApp.play", InvalidSource);

    void Because() => _result = _validation.Validate(_document);

    [Fact] void should_compile_the_document() => _result.FileCount.ShouldEqual(1);
    [Fact] void should_report_an_error() => ScreenplayDiagnostics.HasErrors(_result.Diagnostics).ShouldBeTrue();
    [Fact] void should_point_at_the_file_and_position() => _result.Diagnostics[0].Location.ShouldEqual("MyApp.play(5,5)");
    [Fact] void should_not_invent_a_code() => _result.Diagnostics[0].Code.ShouldBeEmpty();
}
