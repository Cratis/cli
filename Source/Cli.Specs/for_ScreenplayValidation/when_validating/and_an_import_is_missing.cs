// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayValidation.when_validating;

public class and_an_import_is_missing : given.a_folder_with_documents
{
    string _root;
    ValidatedScreenplay _result;

    void Establish() => _root = WriteDocument("root.play", "import \"missing.play\"\n" + ValidSource);

    void Because() => _result = _validation.Validate(_root);

    [Fact] void should_count_only_the_root() => _result.FileCount.ShouldEqual(1);
    [Fact] void should_report_a_missing_import_warning() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == "PLAY0455").Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Warning);
    [Fact] void should_locate_the_import_in_the_root() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == "PLAY0455").Location.ShouldEqual("root.play(1,1)");
}
