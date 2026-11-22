// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.for_PrologueExtractorCommand.when_preparing_the_capture_directory;

public class with_a_missing_json_directory : Specification
{
    string _root;
    string _result;

    void Establish() => _root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    void Because() => _result = PrologueExtractorCommand.PrepareCaptureDirectory(
        new PrologueWizardInput(Guid.NewGuid(), [], [], null, null, new(OutputKind.Json, "", "./captures")),
        Path.Combine(_root, "cratis-prologue.json"));

    void Destroy() => Directory.Delete(_root, true);

    [Fact] void should_create_the_directory() => Directory.Exists(Path.Combine(_root, "captures")).ShouldBeTrue();
    [Fact] void should_return_the_host_directory() => _result.ShouldEqual(Path.Combine(_root, "captures"));
    [Fact] void should_not_make_it_world_writable() => (OperatingSystem.IsWindows() || !File.GetUnixFileMode(_result).HasFlag(UnixFileMode.OtherWrite)).ShouldBeTrue();
}
