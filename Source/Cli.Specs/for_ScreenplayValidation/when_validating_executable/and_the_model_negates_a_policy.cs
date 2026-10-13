// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;

namespace Cratis.Cli.for_ScreenplayValidation.when_validating_executable;

public class and_the_model_negates_a_policy : given.a_folder_with_documents
{
    ValidatedScreenplay _source;
    ValidatedScreenplay _result;

    void Establish()
    {
        foreach (var document in PolicyNegationCorpus.V7.SourceForms.Single(_ => _.Name == "folder").Documents)
        {
            WriteDocument(document.DisplayPath, document.Text);
        }
    }

    void Because()
    {
        _source = _validation.Validate(_folder);
        _result = _validation.ValidateExecutable(_folder);
    }

    [Fact] void should_accept_the_source() => _source.Diagnostics.Where(_ => _.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_bind_the_model() => _result.Executable.ShouldEqual(true);
    [Fact] void should_report_no_binding_errors() => _result.Diagnostics.Where(_ => _.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
}
