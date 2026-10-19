// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ConformNamespaceScreenplayCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_a_matching_namespace : given.a_conform_namespace_command
{
    void Establish()
    {
        Appended(2, _register);
        _settings.FailOnDrift = true;
    }

    async Task Because() => await Run();

    [Fact] void should_succeed() => _exitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_report_no_drift() => Json.GetProperty("hasDrift").GetBoolean().ShouldBeFalse();
    [Fact] void should_report_a_complete_comparison() => Json.GetProperty("complete").GetBoolean().ShouldBeTrue();
}
