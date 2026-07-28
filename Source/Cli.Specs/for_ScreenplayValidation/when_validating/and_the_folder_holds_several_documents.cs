// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayValidation.when_validating;

public class and_the_folder_holds_several_documents : given.a_folder_with_documents
{
    ValidatedScreenplay _result;

    void Establish()
    {
        WriteDocument("MyApp.play", ValidSource);
        WriteDocument(Path.Combine("nested", "Broken.play"), InvalidSource);
    }

    void Because() => _result = _validation.Validate(_folder);

    [Fact] void should_compile_every_document_beneath_it() => _result.FileCount.ShouldEqual(2);
    [Fact] void should_report_the_error_in_the_nested_document() => ScreenplayDiagnostics.HasErrors(_result.Diagnostics).ShouldBeTrue();
    [Fact] void should_point_at_the_document_relative_to_the_folder() => _result.Diagnostics[0].Location.ShouldEqual("nested/Broken.play(5,5)");
}
