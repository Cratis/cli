// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_validating;

[Collection(CliSpecsCollection.Name)]
public class and_no_path_is_given : given.a_validate_screenplay_command
{
    int _result;

    async Task Because() => _result = await Execute();

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_validate_the_current_directory() => _validation.Received(1).Validate(_folder);
}
