// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.New;

namespace Cratis.Cli.for_NewCommand.when_creating_from_a_local_template;

[Collection(CliSpecsCollection.Name)]
public class with_an_unknown_parameter : given.a_local_template
{
    int _exitCode;

    async Task Because() => _exitCode = await New("sample", "--language", "csharp", "-n", "Demo", "-o", _output, "--dry-run", "--format", "json", "--", "--Nope", "x");

    [Fact] void should_not_run() => _exitCode.ShouldEqual(NewCommand.CouldNotRun);
    [Fact] void should_report_invalid_parameters() => _console.ToString().ShouldContain("\"error\": \"invalid parameters\"");
}
