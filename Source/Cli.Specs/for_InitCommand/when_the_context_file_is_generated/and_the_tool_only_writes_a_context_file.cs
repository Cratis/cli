// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_InitCommand.when_the_context_file_is_generated;

/// <summary>
/// Cursor and Windsurf are configured entirely through a rules file, so --no-context leaves them with
/// nothing to write. Reporting that is the whole value: silently doing nothing reads as success.
/// </summary>
public class and_the_tool_only_writes_a_context_file : Specification
{
    string _tempDir;
    IReadOnlyList<string> _cursor;
    IReadOnlyList<string> _windsurf;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    void Because()
    {
        var configuration = new AiToolConfiguration(Force: true, IncludeCommands: true, IncludeContext: false, LlmContextJson: "{}");
        _cursor = AiToolConfigurator.Configure(AiTool.Cursor, _tempDir, configuration);
        _windsurf = AiToolConfigurator.Configure(AiTool.Windsurf, _tempDir, configuration);
    }

    [Fact] void should_not_write_the_cursor_rule() =>
        File.Exists(Path.Combine(_tempDir, ".cursor", "rules", "chronicle.mdc")).ShouldBeFalse();

    [Fact] void should_not_write_the_windsurf_rules() =>
        File.Exists(Path.Combine(_tempDir, ".windsurfrules")).ShouldBeFalse();

    [Fact] void should_say_why_cursor_got_nothing() =>
        _cursor.ShouldContain(_ => _.Contains("Skipped the @CHRONICLE.md reference", StringComparison.Ordinal));

    [Fact] void should_say_why_windsurf_got_nothing() =>
        _windsurf.ShouldContain(_ => _.Contains("Skipped the @CHRONICLE.md reference", StringComparison.Ordinal));

    void Destroy()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}
