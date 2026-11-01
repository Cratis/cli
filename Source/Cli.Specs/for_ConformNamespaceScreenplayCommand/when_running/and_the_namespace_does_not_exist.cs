// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Cli.for_ConformNamespaceScreenplayCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class and_the_namespace_does_not_exist : given.a_conform_namespace_command
{
    void Establish() => _namespaces = ["Other"];

    async Task Because() => await Run();

    [Fact] void should_report_a_validation_error() => _exitCode.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_not_scan_the_sequence() => _observations.DidNotReceive().Tail(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    [Fact] void should_name_the_missing_namespace() => Regex.Unescape(_output + _error).ShouldContain("Namespace 'Default' was not found");
}
