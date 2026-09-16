// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.New;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Cratis.Cli.for_NewSettings;

public class when_validating_conflicting_sources : Specification
{
    ValidationResult _result = null!;

    void Because() => _result = new NewSettings
    {
        Package = "Some.Package",
        TemplatePath = "../local/templates"
    }.Validate();

    [Fact] void should_reject_the_combination() => _result.Successful.ShouldBeFalse();

    [Fact] void should_explain_the_conflict() => _result.Message.ShouldContain("cannot be combined");
}
