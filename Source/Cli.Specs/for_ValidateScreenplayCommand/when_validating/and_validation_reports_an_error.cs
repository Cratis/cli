// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_validating;

[Collection(CliSpecsCollection.Name)]
public class and_validation_reports_an_error : given.a_validate_screenplay_command
{
    int _result;

    void Establish() =>
        _validation
            .Validate(Arg.Any<string>())
            .Returns(new ValidatedScreenplay(
                1,
                [
                    new ScreenplayDiagnostic(ScreenplayDiagnosticSeverity.Warning, string.Empty, "a warning", "MyApp.play(3,1)"),
                    new ScreenplayDiagnostic(ScreenplayDiagnosticSeverity.Error, string.Empty, "an error", "MyApp.play(5,5)")
                ]));

    async Task Because() => _result = await Execute();

    [Fact] void should_fail_with_a_validation_error() => _result.ShouldEqual(ExitCodes.ValidationError);
}
