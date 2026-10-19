// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ConformNamespaceScreenplayCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_partial_coverage_and_fail_on_drift : given.a_conform_namespace_command
{
    void Establish()
    {
        Appended(3, _register);
        _settings.MaxEvents = 1;
        _settings.FailOnDrift = true;
    }

    async Task Because() => await Run();

    [Fact] void should_report_an_inconclusive_comparison() => _exitCode.ShouldEqual(ExitCodes.Inconclusive);
    [Fact] void should_omit_the_drift_verdict() => Json.TryGetProperty("hasDrift", out _).ShouldBeFalse();
    [Fact] void should_report_an_incomplete_comparison() => Json.GetProperty("complete").GetBoolean().ShouldBeFalse();
}
