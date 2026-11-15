// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiToolConfigurator.when_configuring;

/// <summary>
/// When the links cannot be resolved the guard must not authorize a write. Bounded link resolution rejects the
/// cycle, so configuring fails instead of hanging or writing anywhere.
/// </summary>
public class and_the_links_form_a_cycle : Specification
{
    string _project = null!;
    string _corpusPrompts = null!;
    Exception _failure = null!;

    void Establish()
    {
        _project = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _corpusPrompts = Path.Combine(_project, ".cratis", "ai", "prompts");
        Directory.CreateDirectory(_corpusPrompts);
        Directory.CreateDirectory(Path.Combine(_project, ".github"));

        // .github/prompts -> loop/prompts and loop -> .github, so resolving the prompts folder never ends.
        Directory.CreateSymbolicLink(Path.Combine(_project, "loop"), ".github");
        Directory.CreateSymbolicLink(Path.Combine(_project, ".github", "prompts"), Path.Combine("..", "loop", "prompts"));
    }

    void Because() => _failure = Catch.Exception(() => AiToolConfigurator.Configure(AiTool.Copilot, _project, new(Force: true, IncludeCommands: true, IncludeContext: true, LlmContextJson: "{}")));

    [Fact] void should_fail_instead_of_writing() => (_failure is IOException && _failure.Message.StartsWith("Too many levels of symbolic links resolving '", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_write_anything_into_the_corpus() => Directory.EnumerateFileSystemEntries(_corpusPrompts).ShouldBeEmpty();

    void Destroy() => Directory.Delete(_project, true);
}
