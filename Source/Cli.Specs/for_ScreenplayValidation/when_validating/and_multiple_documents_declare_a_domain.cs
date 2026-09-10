// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayValidation.when_validating;

public class and_multiple_documents_declare_a_domain : given.a_folder_with_documents
{
    ValidatedScreenplay _result;

    void Establish()
    {
        WriteDocument("First.play", ValidSource);
        WriteDocument("nested/Second.play", "\ndomain Lending\n");
    }

    void Because() => _result = _validation.Validate(_folder);

    [Fact] void should_count_both_files() => _result.FileCount.ShouldEqual(2);
    [Fact] void should_report_the_compiler_conflict_code() => _result.Diagnostics.Single().Code.ShouldEqual("PLAY0172");
    [Fact] void should_point_to_the_conflicting_domain() => _result.Diagnostics.Single().Location.ShouldEqual("nested/Second.play(2,1)");
    [Fact] void should_fail_validation() => ScreenplayDiagnostics.ExitCodeFor(_result.Diagnostics).ShouldEqual(ExitCodes.ValidationError);
}
