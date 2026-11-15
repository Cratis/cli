// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.New;

namespace Cratis.Cli.for_NewCommand.when_creating_from_a_local_template;

[Collection(CliSpecsCollection.Name)]
public class with_an_unknown_template : given.a_local_template
{
    int _exitCode;

    async Task Because() => _exitCode = await New("missing", "--language", "csharp", "-n", "Demo", "-o", _output, "--dry-run", "--format", "json");

    [Fact] void should_not_run() => _exitCode.ShouldEqual(NewCommand.CouldNotRun);
    [Fact] void should_report_the_template_as_not_found() => _console.ToString().ShouldContain("\"error\": \"template not found\"");
    [Fact] void should_list_the_available_templates() => _console.ToString().ShouldContain("was found for csharp. Available: sample.");
}
