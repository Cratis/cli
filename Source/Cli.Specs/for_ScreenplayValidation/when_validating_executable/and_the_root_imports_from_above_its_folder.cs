// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayValidation.when_validating_executable;

public class and_the_root_imports_from_above_its_folder : given.a_folder_with_documents
{
    string _root;
    ValidatedScreenplay _source;
    ValidatedScreenplay _result;

    void Establish()
    {
        WriteDocument("Shared.play", "concept ProjectId : Uuid\n");
        _root = WriteDocument("model/application.play", "import \"../Shared.play\"\n");
    }

    void Because()
    {
        _source = _validation.Validate(_root);
        _result = _validation.ValidateExecutable(_root);
    }

    [Fact] void should_compile_the_source() => _source.Diagnostics.Where(_ => _.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_not_be_executable() => _result.Executable.ShouldEqual(false);
    [Fact] void should_report_the_document_above_the_root() => _result.Diagnostics.Single(_ => _.Code == ScreenplayBinding.OutsideRootCode).Message.ShouldContain("../Shared.play");
}
