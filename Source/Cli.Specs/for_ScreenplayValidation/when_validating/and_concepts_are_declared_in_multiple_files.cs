// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayValidation.when_validating;

public class and_concepts_are_declared_in_multiple_files : given.a_folder_with_documents
{
    ValidatedScreenplay _result;

    void Establish()
    {
        WriteDocument("z.play", "\nconcept BookId : String\n");
        WriteDocument("B.play", ConceptsSource);
        WriteDocument("A.play", ConceptsSource);
    }

    void Because() => _result = _validation.Validate(_folder);

    [Fact] void should_count_all_discovered_files() => _result.FileCount.ShouldEqual(3);
    [Fact] void should_report_duplicate_and_conflicting_declarations() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ToArray().ShouldEqual(["PLAY0173", "PLAY0173"]);
    [Fact] void should_fail_validation() => ScreenplayDiagnostics.ExitCodeFor(_result.Diagnostics).ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_order_diagnostics_by_ordinal_path_not_creation_order() => _result.Diagnostics.Select(diagnostic => diagnostic.Location).ToArray().ShouldEqual(["B.play(1,1)", "z.play(2,1)"]);
    [Fact] void should_identify_the_first_declaration() => _result.Diagnostics.Select(diagnostic => diagnostic.Message).Distinct().Single().ShouldEqual("Duplicate declaration of 'BookId' - already declared in 'A.play'");
}
