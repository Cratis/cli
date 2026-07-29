// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayValidation.when_validating;

public class and_the_document_is_valid : given.a_folder_with_documents
{
    string _document;
    ValidatedScreenplay _result;

    void Establish() => _document = WriteDocument("MyApp.play", ValidSource);

    void Because() => _result = _validation.Validate(_document);

    [Fact] void should_compile_the_document() => _result.FileCount.ShouldEqual(1);
    [Fact] void should_not_report_anything() => _result.Diagnostics.ShouldBeEmpty();
}
