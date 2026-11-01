// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Cli.Commands.Ai.Testing;

namespace Cratis.Cli.for_AiTestRunner.given;

public class a_conformance_run : for_AiCorpusSynchronizer.given.a_corpus_with_profile_scoped_rules
{
    private protected AiTestRunner _runner = null!;
    private protected AiTestReport _report = null!;
    protected AiTestSettings _settings = null!;
    private protected AiTestFixture _fixture = null!;
    private protected IAiTestHarness _harness = null!;
    private protected IAiTestProcess _process = null!;
    protected string _fixtureDirectory = string.Empty;
    protected string _workspace = string.Empty;

    void Establish()
    {
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "manifest.json"), "{\"harnesses\":[\"claude\",\"codex\"],\"profiles\":[\"cratis/example\"],\"languages\":[\"csharp\"]}");
        AiCorpusSynchronizer.Synchronize(_project, _corpus, new(["claude"], ["cratis/example"], ["csharp"]));
        _fixtureDirectory = Path.Combine(_corpus, "Evaluations", "conformance", "example");
        Directory.CreateDirectory(Path.Combine(_fixtureDirectory, "input"));
        File.WriteAllText(Path.Combine(_fixtureDirectory, "input", "input.txt"), "original input");
        _fixture = new() { Name = "example", Prompt = "Write result.txt", Profiles = ["cratis/example"], Languages = ["csharp"], ExpectedInstructionPaths = [".cratis/ai/rules/general.md"], Checks = [new() { Type = "file_exists", Path = "result.txt" }] };
        _settings = new() { Source = _corpus };
        _harness = Substitute.For<IAiTestHarness>();
        _harness.Id.Returns("claude");
        _harness.Run(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<AiTestBounds>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            _workspace = call.ArgAt<string>(0);
            await File.WriteAllTextAsync(Path.Combine(_workspace, "result.txt"), "correct result");
            return new AiTestHarnessResult("passed", Version: "fake 1", Transcript: "sk-private ghp_private Bearer private");
        });
        _process = Substitute.For<IAiTestProcess>();
        _process.Run(Arg.Any<AiTestProcessRequest>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new AiTestProcessResult("completed", 0, "command evidence")));
        _runner = new([_harness], _process, new([]));
    }

    private protected async Task<AiTestReport> Run()
    {
        await File.WriteAllTextAsync(Path.Combine(_fixtureDirectory, "fixture.json"), JsonSerializer.Serialize(_fixture, AiTestFixture.Json));
        return await _runner.Run(_project, _corpus, _settings, CancellationToken.None);
    }
}
