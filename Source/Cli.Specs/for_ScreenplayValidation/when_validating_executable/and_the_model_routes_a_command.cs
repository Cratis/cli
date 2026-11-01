// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayValidation.when_validating_executable;

public class and_the_model_routes_a_command : given.a_folder_with_documents
{
    ValidatedScreenplay _source = null!;
    ValidatedScreenplay _result = null!;

    void Establish() => WriteDocument("Routes.play", for_ScreenplayPlanning.when_planning_an_esm_v8_model.RoutedSource);

    void Because()
    {
        _source = _validation.Validate(_folder);
        _result = _validation.ValidateExecutable(_folder);
    }

    [Fact] void should_validate_the_routed_source() => _source.Diagnostics.Where(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_bind_the_command_and_specification_routes() => _result.Executable.ShouldEqual(true);
    [Fact] void should_report_no_binding_errors() => _result.Diagnostics.Where(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
}
