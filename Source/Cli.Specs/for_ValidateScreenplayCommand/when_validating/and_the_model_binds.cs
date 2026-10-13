// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_validating;

[Collection(CliSpecsCollection.Name)]
public class and_the_model_binds : given.a_validate_screenplay_command
{
    int _result;

    void Establish()
    {
        _settings.Executable = true;
        _validation.ValidateExecutable(Arg.Any<string>()).Returns(new ValidatedScreenplay(1, []) { Executable = true });
    }

    async Task Because() => _result = await Execute();

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
}
