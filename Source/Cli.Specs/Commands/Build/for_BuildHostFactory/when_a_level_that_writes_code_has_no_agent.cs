// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuildHostFactory;

public class when_a_level_that_writes_code_has_no_agent : Specification
{
    string _project;
    Exception _codeWithAi;
    Exception _ai;

    void Establish()
    {
        _project = Path.Combine(Path.GetTempPath(), $"cratis-factory-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_project);
    }

    void Destroy() => Directory.Delete(_project, true);

    async Task Because()
    {
        _codeWithAi = await Catch.Exception(() => BuildHostFactory.Create(Settings("code-ai"), null, false, null, CancellationToken.None));
        _ai = await Catch.Exception(() => BuildHostFactory.Create(Settings("ai"), null, false, null, CancellationToken.None));
    }

    [Fact] void should_refuse_code_ai() => _codeWithAi.ShouldBeOfExactType<ArgumentException>();
    [Fact] void should_say_code_ai_needs_an_agent() => _codeWithAi.Message.ShouldContain("The code-ai level needs the agent");
    [Fact] void should_refuse_ai() => _ai.ShouldBeOfExactType<ArgumentException>();
    [Fact] void should_say_ai_needs_an_agent() => _ai.Message.ShouldContain("The ai level needs the agent that implements the slices from the model: name it with --agent");

    BuildRunSettings Settings(string level) => new() { Level = level, Project = _project, NoGuidance = true };
}
