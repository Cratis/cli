// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_printing_the_gate;

[Collection(CliSpecsCollection.Name)]
public class without_compiling : given.a_validate_screenplay_command
{
    int _result;
    void Establish() => _settings.PrintGate = true;
    async Task Because() => _result = await Execute();
    [Fact] void should_succeed() => _result.ShouldEqual(0);
    [Fact] void should_not_call_validation() => _validation.ReceivedCalls().ShouldBeEmpty();
}
