// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayCompilationLoader.when_loading;

public class after_cancellation : Specification
{
    readonly List<string> _messages = [];

    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ScreenplayCompilationLoader.Load(
            new ScreenplayCompilationRequest("/workspace/Application.slnx", false, null),
            cancellation.Token,
            _messages.Add));
    }

    [Fact] void should_not_report_any_stage() => _messages.ShouldBeEmpty();
}
