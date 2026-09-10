// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ProviderScreenplayGeneration.when_generating;

public class and_a_supported_provider_receives_an_invalid_feature_root : given.an_application_scope
{
    IScreenplaySourceProvider _provider;
    LoadedCompilation _loaded;
    GeneratedScreenplay _result;

    void Establish()
    {
        _loaded = LoadedFrom(Project("Persistence", MartenPackages, false, MartenSource));
        _provider = Substitute.For<IScreenplaySourceProvider>();
        _provider.Name.Returns(ScreenplayProviders.Marten);
        _provider.Version.Returns("1.0.0");
        _provider.Supersedes.Returns([]);
    }

    async Task Because()
    {
        var generation = new ProviderScreenplayGeneration(
            [_provider],
            (_, _, _) => Task.FromResult(_loaded));
        _result = await generation.Generate(
            "/private/checkout/Application.slnx",
            ScreenplayGenerationOptions.Default with
            {
                Provider = ScreenplayProviders.Marten,
                FeatureRoot = "/private/checkout/Features"
            },
            CancellationToken.None);
    }

    [Fact] void should_not_select_source_for_provenance() => _provider.DidNotReceive().SelectFrom(Arg.Any<LoadedCompilation>());
    [Fact] void should_not_call_generation() => _provider.DidNotReceive().GenerateFrom(Arg.Any<LoadedCompilation>(), Arg.Any<string>(), Arg.Any<ScreenplayGenerationOptions>());
    [Fact] void should_publish_no_provenance() => _result.Provenance.ShouldBeNull();
    [Fact] void should_generate_no_source() => _result.Source.ShouldBeEmpty();
}
