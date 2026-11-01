// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Cli.for_ConformNamespaceScreenplayCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class and_the_namespace_differs_only_in_case : given.a_conform_namespace_command
{
    void Establish() => _namespaces = ["default"];

    async Task Because() => await Run();

    [Fact] void should_report_a_validation_error() => _exitCode.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_suggest_the_similar_name() => Regex.Unescape(_output + _error).ShouldContain("did you mean 'default'");
}
