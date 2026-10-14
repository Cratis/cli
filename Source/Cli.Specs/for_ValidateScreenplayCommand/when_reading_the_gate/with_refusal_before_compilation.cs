// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_reading_the_gate;

[Collection(CliSpecsCollection.Name)]
public class with_refusal_before_compilation : given.a_validate_screenplay_command
{
    int _result;
    void Establish() => File.WriteAllText(Path.Combine(_folder, "screenplay.json"), "{broken");
    async Task Because() => _result = await Execute();
    [Fact] void should_refuse_input() => _result.ShouldEqual(1);
    [Fact] void should_not_call_validation() => _validation.ReceivedCalls().ShouldBeEmpty();
}
