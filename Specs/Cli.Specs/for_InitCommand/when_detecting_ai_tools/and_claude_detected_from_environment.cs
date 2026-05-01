// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_InitCommand.when_detecting_ai_tools;

[Collection(CliSpecsCollection.Name)]
public class and_claude_detected_from_environment : Specification
{
    string _tempDir;
    string? _previousValue;
    IReadOnlyList<AiTool> _result;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _previousValue = Environment.GetEnvironmentVariable("CLAUDECODE");
        Environment.SetEnvironmentVariable("CLAUDECODE", "1");
    }

    void Because() => _result = AiToolDetector.Detect(_tempDir);

    [Fact] void should_detect_claude_without_project_files() => _result.ShouldContain(AiTool.Claude);

    void Destroy()
    {
        Environment.SetEnvironmentVariable("CLAUDECODE", _previousValue);
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}
