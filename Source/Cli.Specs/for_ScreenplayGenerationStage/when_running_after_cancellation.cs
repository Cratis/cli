// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayGenerationStage;

public class when_running_after_cancellation : Specification
{
    readonly List<string> _messages = [];
    bool _worked;

    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ScreenplayGenerationStage.Run(
            ScreenplayGenerationStage.Generation,
            _messages.Add,
            () =>
            {
                _worked = true;
                return Task.FromResult(0);
            },
            cancellation.Token));
    }

    [Fact] void should_not_report_any_stage() => _messages.ShouldBeEmpty();
    [Fact] void should_not_start_the_work() => _worked.ShouldBeFalse();
}
