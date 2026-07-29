// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayValidation.when_validating;

public class and_the_folder_holds_no_documents : given.a_folder_with_documents
{
    ValidatedScreenplay _result;

    void Because() => _result = _validation.Validate(_folder);

    [Fact] void should_compile_nothing() => _result.FileCount.ShouldEqual(0);
    [Fact] void should_not_report_anything() => _result.Diagnostics.ShouldBeEmpty();
}
