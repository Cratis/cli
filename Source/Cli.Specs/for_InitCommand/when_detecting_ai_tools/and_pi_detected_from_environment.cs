// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_InitCommand.when_detecting_ai_tools;

/// <summary>
/// Pi exports its PI_* variables into every command it runs, which is what lets a first `cratis init` from
/// inside a Pi session configure Pi even though the project carries no .pi directory yet.
/// </summary>
[Collection(CliSpecsCollection.Name)]
public class and_pi_detected_from_environment : Specification
{
    string _tempDir;
    string? _previousValue;
    IReadOnlyList<AiTool> _result;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _previousValue = Environment.GetEnvironmentVariable("PI_CODING_AGENT");
        Environment.SetEnvironmentVariable("PI_CODING_AGENT", "1");
    }

    void Because() => _result = AiToolDetector.Detect(_tempDir);

    [Fact] void should_detect_pi_without_project_files() => _result.ShouldContain(AiTool.Pi);

    void Destroy()
    {
        Environment.SetEnvironmentVariable("PI_CODING_AGENT", _previousValue);
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}
