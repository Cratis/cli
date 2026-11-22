// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_a_managed_prompt_link_is_missing : given.a_pi_corpus
{
    string _path;
    string _target;

    void Establish()
    {
        var prompts = Path.Combine(_corpus, ".cratis", "ai", "prompts");
        Directory.CreateDirectory(prompts);
        File.WriteAllText(Path.Combine(prompts, "example.prompt.md"), "# Prompt");
        AiCorpusSynchronizer.Synchronize(_project, _corpus, Configuration);
        _path = Path.Combine(_project, ".pi", "prompts", "example.md");
        _target = new FileInfo(_path).LinkTarget;
        File.Delete(_path);
    }

    void Because() => _result = AiCorpusSynchronizer.Synchronize(_project, _corpus, Configuration);

    [Fact] void should_restore_the_recorded_target() => new FileInfo(_path).LinkTarget.ShouldEqual(_target);
    [Fact] void should_report_the_restoration() => _result.Actions.ShouldContain("Restored .pi/prompts/example.md");
    [Fact] void should_not_report_a_conflict() => _result.Conflicts.ShouldBeEmpty();
}
