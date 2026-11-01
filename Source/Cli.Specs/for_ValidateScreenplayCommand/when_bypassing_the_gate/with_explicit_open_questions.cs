// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_bypassing_the_gate;

public class with_explicit_open_questions : given.a_gate_with_questions
{
    Task Because() => Run("--no-gate-file", "--open-questions", "fail");
    [Fact] void should_read_questions_from_the_resolved_root() => _exitCode.ShouldEqual(5);
    [Fact] void should_use_the_explicit_mode() => SourceOf("openQuestions").ShouldEqual("commandLine");
    [Fact] void should_not_inherit_file_warnings() => SourceOf("warningsAsErrors").ShouldEqual("default");
}
