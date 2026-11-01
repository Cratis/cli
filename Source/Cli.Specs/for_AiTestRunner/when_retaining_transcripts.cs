// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiTestRunner;

public class when_retaining_transcripts : given.a_conformance_run
{
    string _transcript = string.Empty;
    void Establish() => _settings.KeepTranscripts = true;
    async Task Because()
    {
        _report = await Run();
        _transcript = await File.ReadAllTextAsync(_report.Results.Single().TranscriptPath!);
    }
    [Fact] void should_redact_secret_looking_values() => _transcript.ShouldNotContain("private");
    [Fact] void should_write_only_under_temporary_output() => Path.GetDirectoryName(_report.Results.Single().TranscriptPath).ShouldEqual(Path.Combine(Path.GetDirectoryName(_workspace)!, "output"));
    void Destroy() => Directory.Delete(Path.GetDirectoryName(_workspace)!, true);
}
