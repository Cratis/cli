// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ProviderScreenplayGeneration.when_generating;

public class and_stages_are_reported : given.an_application_scope
{
    readonly List<string> _messages = [];

    async Task Because()
    {
        var loaded = LoadedFrom(Project("Persistence", MartenPackages, false, MartenSource));
        var generation = new ProviderScreenplayGeneration(
            ScreenplaySourceProviders.Default,
            (_, _, _, report) =>
            {
                report($"{ScreenplayGenerationStage.Workspace}: started");
                report($"{ScreenplayGenerationStage.Workspace}: completed");
                return Task.FromResult(loaded);
            });
        await generation.Generate(
            "/workspace/Application.slnx",
            ScreenplayGenerationOptions.Default with { Provider = ScreenplayProviders.Marten },
            _messages.Add,
            CancellationToken.None);
    }

    [Fact]
    void should_report_loading_before_generation() =>
        _messages.ToArray().ShouldEqual(
        [
            $"{ScreenplayGenerationStage.Workspace}: started",
            $"{ScreenplayGenerationStage.Workspace}: completed",
            $"{ScreenplayGenerationStage.Generation}: started",
            $"{ScreenplayGenerationStage.Generation}: completed"
        ]);
}
