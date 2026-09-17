// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.New;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Cratis.Cli.for_NewSettings;

public class when_validating_the_output_format : Specification
{
    [Fact] void should_accept_table() => Subject("table").Successful.ShouldBeTrue();
    [Fact] void should_accept_plain() => Subject("plain").Successful.ShouldBeTrue();
    [Fact] void should_accept_json() => Subject("json").Successful.ShouldBeTrue();
    [Fact] void should_reject_xml() => Subject("xml").Successful.ShouldBeFalse();

    static ValidationResult Subject(string format) => new NewSettings { Format = format }.Validate();
}
