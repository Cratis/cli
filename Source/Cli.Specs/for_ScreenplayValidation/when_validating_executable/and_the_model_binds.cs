// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;

namespace Cratis.Cli.for_ScreenplayValidation.when_validating_executable;

public class and_the_model_binds : given.a_folder_with_documents
{
    ValidatedScreenplay _result;

    void Establish()
    {
        foreach (var document in RegisterProjectCorpus.LegacyV1.SourceForms.Single(_ => _.Name == "folder").Documents)
        {
            WriteDocument(document.DisplayPath, document.Text);
        }
    }

    void Because() => _result = _validation.ValidateExecutable(_folder);

    [Fact] void should_be_executable() => _result.Executable.ShouldEqual(true);
    [Fact] void should_report_no_errors() => _result.Diagnostics.Where(_ => _.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_not_report_a_diagnostic_twice() => _result.Diagnostics.Distinct().Count().ShouldEqual(_result.Diagnostics.Count);
}
