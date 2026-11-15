// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Cli.for_ScreenplayCompilationProgress;

public class when_loading_in_order : Specification
{
    readonly List<string> _messages = [];

    async Task Because()
    {
        using var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("Application", LanguageNames.CSharp);
        using var progress = new ScreenplayCompilationProgress(_messages.Add, CancellationToken.None);
        await progress.CompleteWorkspace(() => Task.FromResult(0));
        var prepared = progress.PrepareResources(project);
        await progress.Compile(prepared);
    }

    [Fact]
    void should_report_workspace_resources_and_compilation_stages_in_order() =>
        _messages.ToArray().ShouldEqual(
        [
            $"{ScreenplayGenerationStage.Workspace}: started",
            $"{ScreenplayGenerationStage.Workspace}: completed",
            $"{ScreenplayGenerationStage.Resources}: started",
            $"{ScreenplayGenerationStage.Resources}: completed",
            $"{ScreenplayGenerationStage.Compilation}: started",
            $"{ScreenplayGenerationStage.Compilation}: completed"
        ]);
}
