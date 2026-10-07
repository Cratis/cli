// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayValidation.when_validating_executable;

public class and_the_model_uses_an_event_stream : given.a_folder_with_documents
{
    const string Source = "eventsource Account\n  stream Transactions\n";
    ValidatedScreenplay _source;
    ValidatedScreenplay _result;

    void Establish() => WriteDocument("Accounts.play", Source);

    void Because()
    {
        _source = _validation.Validate(_folder);
        _result = _validation.ValidateExecutable(_folder);
    }

    [Fact] void should_be_valid_source() => _source.Diagnostics.Where(_ => _.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_not_check_binding_without_the_option() => _source.Executable.ShouldBeNull();
    [Fact] void should_not_be_executable() => _result.Executable.ShouldEqual(false);
    [Fact] void should_report_the_unadmitted_construct() => _result.Diagnostics.Any(_ => _.Code == "PLAY0268" && _.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeTrue();
    [Fact] void should_locate_it_in_the_document() => _result.Diagnostics.First(_ => _.Code == "PLAY0268").Location!.StartsWith("Accounts.play(", StringComparison.Ordinal).ShouldBeTrue();
}
