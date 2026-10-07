// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_validating;

[Collection(CliSpecsCollection.Name)]
public class and_the_model_does_not_bind : given.a_validate_screenplay_command
{
    int _result;

    void Establish()
    {
        _settings.Executable = true;
        _validation
            .ValidateExecutable(Arg.Any<string>())
            .Returns(new ValidatedScreenplay(1, [new ScreenplayDiagnostic(ScreenplayDiagnosticSeverity.Error, "PLAY0268", "not admitted", "MyApp.play(1,1)")]) { Executable = false });
    }

    async Task Because() => _result = await Execute();

    [Fact] void should_fail_with_a_validation_error() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_bind_the_model() => _validation.Received(1).ValidateExecutable(_folder);
    [Fact] void should_not_run_the_source_only_validation() => _validation.DidNotReceive().Validate(Arg.Any<string>());
}
