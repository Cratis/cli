// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Cli.Commands.Ai.Testing;

namespace Cratis.Cli.for_AiTestRunner;

public class when_installing_fixture_slices : given.a_conformance_run
{
    readonly List<AiConfiguration> _installed = [];
    AiTestReport _normal = null!;
    AiTestReport _source = null!;

    void Establish()
    {
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "manifest.json"), "{\"harnesses\":[\"claude\"],\"profiles\":[\"cratis/example\",\"cratis/example-other\",\"cratis/example/child\"],\"languages\":[\"csharp\",\"typescript\",\"javascript\"]}");
        var rules = Path.Combine(_corpus, ".cratis", "ai", "rules");
        File.WriteAllText(Path.Combine(rules, "csharp.md"), "---\napplyTo: \"**/*.cs\"\n---\nC# rules");
        File.WriteAllText(Path.Combine(rules, "typescript.md"), "---\napplyTo: \"**/*.ts\"\n---\nTypeScript rules");
        File.WriteAllText(Path.Combine(rules, "other.md"), "---\nprofile: cratis/example-other\n---\nOther profile rules");
        _fixture = _fixture with { Languages = ["typescript"], Checks = [
            new() { Type = "file_absent", Path = ".cratis/ai/rules/csharp.md", Phase = AiTestPhase.Install },
            new() { Type = "file_exists", Path = ".cratis/ai/rules/typescript.md", Phase = AiTestPhase.Install },
            new() { Type = "file_absent", Path = ".cratis/ai/rules/other.md", Phase = AiTestPhase.Install },
            new() { Type = "file_exists", Path = "result.txt" }
        ] };
        _harness.Run(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<AiTestBounds>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            _workspace = call.ArgAt<string>(0);
            _installed.Add(JsonSerializer.Deserialize<AiConfiguration>(await File.ReadAllTextAsync(Path.Combine(_workspace, ".cratis", "ai.json")), JsonSerializerOptions.Web)!);
            await File.WriteAllTextAsync(Path.Combine(_workspace, "result.txt"), "done");
            return new AiTestHarnessResult("passed");
        });
    }

    async Task Because()
    {
        AiCorpusSynchronizer.Synchronize(_project, _corpus, new(["claude"], ["cratis/example", "cratis/example-other"], ["csharp", "typescript"]));
        _settings.Source = null;
        _fixture = _fixture with { Profiles = ["cratis/example", "cratis/example/child"], Languages = ["typescript", "javascript"] };
        _normal = await Run();
        AiCorpusSynchronizer.Synchronize(_project, _corpus, new(["claude"], ["cratis/example-other"], ["csharp"]));
        _settings.Source = _corpus;
        _fixture = _fixture with { Profiles = ["cratis/example"], Languages = ["typescript"] };
        _source = await Run();
    }
    [Fact] void should_exclude_csharp_from_normal_mode_ts_fixture() => _normal.Results.Single().Checks[0].Status.ShouldEqual("passed");
    [Fact] void should_exclude_csharp_from_source_mode_ts_fixture() => _source.Results.Single().Checks[0].Status.ShouldEqual("passed");
    [Fact] void should_pass_normal_slice_checks() => _normal.Results.Single().Status.ShouldEqual("passed");
    [Fact] void should_pass_source_slice_checks_despite_invoking_selection() => _source.Results.Single().Status.ShouldEqual("passed");
    [Fact] void should_intersect_normal_profiles() => _installed[0].Profiles.Single().ShouldEqual("cratis/example");
    [Fact] void should_intersect_normal_languages() => _installed[0].Languages.Single().ShouldEqual("typescript");
    [Fact] void should_use_source_declared_profiles() => _installed[1].Profiles.Single().ShouldEqual("cratis/example");
    [Fact] void should_use_source_declared_languages() => _installed[1].Languages.Single().ShouldEqual("typescript");
    [Fact] void should_install_only_harness_under_test() => _installed.TrueForAll(configuration => configuration.Harnesses.SequenceEqual(["claude"])).ShouldBeTrue();
}
