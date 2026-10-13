// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayValidation.when_validating_executable;

public class and_a_root_file_imports_the_model : given.a_folder_with_documents
{
    string _root;
    ValidatedScreenplay _result;

    void Establish()
    {
        _root = WriteDocument("application.play", "import \"accounts/*.play\"\n");
        WriteDocument("accounts/Accounts.play", "eventsource Account\n  stream Transactions\n");
        WriteDocument("unrelated/Other.play", "domain Other\n");
    }

    void Because() => _result = _validation.ValidateExecutable(_root);

    [Fact] void should_bind_the_imported_documents() => _result.Executable.ShouldEqual(true);
    [Fact] void should_report_no_binding_errors() => _result.Diagnostics.Where(_ => _.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
}
