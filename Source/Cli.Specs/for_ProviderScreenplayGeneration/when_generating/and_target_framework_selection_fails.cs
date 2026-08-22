// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ProviderScreenplayGeneration.when_generating;

public class and_target_framework_selection_fails : Specification
{
    const string TargetPath = "/workspace/Application.slnx";
    const string RequestedFramework = "net9.0";

    TrackingProvider _provider;
    LoadedCompilation _failedLoad;
    GeneratedScreenplay _result;
    string? _frameworkPassedToLoader;

    void Establish()
    {
        _provider = new TrackingProvider();
        _failedLoad = LoadedCompilation.Failed(
            ScreenplayDiagnosticCodes.AmbiguousTargetFramework,
            "Project 'Application' targets multiple frameworks: net8.0, net9.0. Pass --framework <TFM> to select one",
            TargetPath);
    }

    async Task Because()
    {
        var generation = new ProviderScreenplayGeneration([_provider], Load);
        _result = await generation.Generate(
            TargetPath,
            ScreenplayGenerationOptions.Default with
            {
                Provider = ScreenplayProviders.Arc,
                TargetFramework = RequestedFramework
            },
            CancellationToken.None);
    }

    Task<LoadedCompilation> Load(string targetPath, string? targetFramework, CancellationToken cancellationToken)
    {
        _frameworkPassedToLoader = targetFramework;
        return Task.FromResult(_failedLoad);
    }

    [Fact] void should_pass_the_requested_framework_to_the_loader() => _frameworkPassedToLoader.ShouldEqual(RequestedFramework);
    [Fact] void should_propagate_the_selection_error() => _result.Diagnostics.Single().Code.ShouldEqual(ScreenplayDiagnosticCodes.AmbiguousTargetFramework);
    [Fact] void should_generate_no_source() => _result.Source.ShouldBeEmpty();
    [Fact] void should_report_no_provenance() => _result.Provenance.ShouldBeNull();
    [Fact] void should_not_interpret_the_source() => _provider.DidGenerate.ShouldBeFalse();
    [Fact] void should_not_select_provider_projects() => _provider.DidSelect.ShouldBeFalse();

    sealed class TrackingProvider : IScreenplaySourceProvider
    {
        public string Name => ScreenplayProviders.Arc;
        public string Version => "1.0.0";
        public IReadOnlyList<string> Supersedes => [];
        public bool RequiresSingleHost => false;
        public bool DidSelect { get; private set; }
        public bool DidGenerate { get; private set; }

        public bool Matches(LoadedCompilation loaded) => true;

        public LoadedCompilation SelectFrom(LoadedCompilation loaded)
        {
            DidSelect = true;
            return loaded;
        }

        public GeneratedScreenplay GenerateFrom(
            LoadedCompilation loaded,
            string targetPath,
            ScreenplayGenerationOptions options)
        {
            DidGenerate = true;
            return new GeneratedScreenplay("generated", []);
        }
    }
}
