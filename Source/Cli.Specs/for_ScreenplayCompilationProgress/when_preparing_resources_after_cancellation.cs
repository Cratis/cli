// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Cli.for_ScreenplayCompilationProgress;

public class when_preparing_resources_after_cancellation : Specification
{
    readonly List<string> _messages = [];

    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        using var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("Application", LanguageNames.CSharp);
        using var progress = new ScreenplayCompilationProgress(_messages.Add, cancellation.Token);
        await cancellation.CancelAsync();
        Assert.ThrowsAny<OperationCanceledException>(() => progress.PrepareResources(project));
    }

    [Fact] void should_only_report_the_stage_that_was_already_started() =>
        _messages.ToArray().ShouldEqual(["Workspace/project loading (including design-time resources): started", "Workspace/project loading (including design-time resources): incomplete"]);
}
