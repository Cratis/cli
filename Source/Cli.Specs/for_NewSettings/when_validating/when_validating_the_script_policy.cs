// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.New;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Cratis.Cli.for_NewSettings;

public class when_validating_the_script_policy : Specification
{
    [Fact] void should_accept_yes() => Subject("yes").Successful.ShouldBeTrue();
    [Fact] void should_accept_no() => Subject("no").Successful.ShouldBeTrue();
    [Fact] void should_accept_prompt() => Subject("prompt").Successful.ShouldBeTrue();
    [Fact] void should_reject_anything_else() => Subject("maybe").Successful.ShouldBeFalse();

    static ValidationResult Subject(string policy) => new NewSettings { AllowScripts = policy }.Validate();
}
