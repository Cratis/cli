// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayValidation.when_validating;

public class and_an_imported_document_has_an_error : given.a_folder_with_documents
{
    string _root;
    ValidatedScreenplay _result;

    void Establish()
    {
        _root = WriteDocument("root.play", "import \"parts/*.play\"\n");
        WriteDocument("parts/broken.play", InvalidSource);
    }

    void Because() => _result = _validation.Validate(_root);

    [Fact] void should_count_both_documents() => _result.FileCount.ShouldEqual(2);
    [Fact] void should_report_the_imported_error() => ScreenplayDiagnostics.HasErrors(_result.Diagnostics).ShouldBeTrue();
    [Fact] void should_locate_the_error_in_the_imported_file() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == "PLAY0027").Location.ShouldEqual("parts/broken.play(5,5)");
}
