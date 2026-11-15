// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_NewCommand.when_creating_from_a_local_template;

[Collection(CliSpecsCollection.Name)]
public class with_the_parameters_option : given.a_local_template
{
    int _exitCode;

    async Task Because() => _exitCode = await New("sample", "--language", "csharp", "--parameters", "--format", "json");

    [Fact] void should_succeed() => _exitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_describe_the_parameters() => _console.ToString().ShouldContain("Greeting");
    [Fact] void should_write_nothing() => Directory.Exists(_output).ShouldBeFalse();
}
