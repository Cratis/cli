// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ConformNamespaceScreenplayCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class and_the_scan_is_cancelled : given.a_conform_namespace_command
{
    Exception? _failure;
    CancellationTokenSource _source = null!;

    void Establish()
    {
        _source = new CancellationTokenSource();
        Appended(3, _register);
    }

    async Task Because()
    {
        await _source.CancelAsync();
        _failure = await Catch.Exception(() => Run(_source.Token));
    }

    void Destroy() => _source.Dispose();

    [Fact] void should_report_cancellation() => (_failure is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_not_report_a_read_failure() => (_failure is ChronicleReadFailed).ShouldBeFalse();
    [Fact] void should_not_write_a_report() => _output.ShouldBeEmpty();
}
