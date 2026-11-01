// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ConformNamespaceScreenplayCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class and_the_scan_is_cancelled : given.a_conform_namespace_command
{
    CancellationTokenSource _source = null!;

    void Establish()
    {
        _source = new CancellationTokenSource();
        Appended(3, _register);
    }

    async Task Because()
    {
        await _source.CancelAsync();
        await Run(_source.Token);
    }

    void Destroy() => _source.Dispose();

    [Fact] void should_be_inconclusive() => _exitCode.ShouldEqual(ExitCodes.Inconclusive);
    [Fact] void should_say_the_scan_was_interrupted() => (_output + _error).ShouldContain("interrupted");
    [Fact] void should_not_write_a_report() => (_output + _error).ShouldNotContain("findings");
}
