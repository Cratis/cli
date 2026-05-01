// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_InitCommand.when_detecting_ai_tools;

public class and_multiple_tools_configured : Specification
{
    string _tempDir;
    IReadOnlyList<AiTool> _result;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        Directory.CreateDirectory(Path.Combine(_tempDir, ".claude"));
        Directory.CreateDirectory(Path.Combine(_tempDir, ".cursor"));
    }

    void Because() => _result = AiToolDetector.Detect(_tempDir);

    [Fact] void should_detect_claude() => _result.ShouldContain(AiTool.Claude);
    [Fact] void should_detect_cursor() => _result.ShouldContain(AiTool.Cursor);

    void Destroy()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}
