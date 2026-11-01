// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ExecutableArtifacts;

public class when_marking_a_script_readable_only_by_its_owner : Specification
{
    const UnixFileMode OwnerReadWrite = UnixFileMode.UserRead | UnixFileMode.UserWrite;

    string _folder = null!;
    string _path = null!;

    void Establish()
    {
        _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"cli-executable-{Guid.NewGuid():N}")).FullName;
        _path = Path.Combine(_folder, "mvnw");
        File.WriteAllText(_path, "#!/bin/sh\n");
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(_path, OwnerReadWrite);
        }
    }

    void Because() => ExecutableArtifacts.Apply(_path, "mvnw");

    [Fact] void should_grant_only_the_owner_execute() => (OperatingSystem.IsWindows() || File.GetUnixFileMode(_path) == (OwnerReadWrite | UnixFileMode.UserExecute)).ShouldBeTrue();

    void Destroy() => Directory.Delete(_folder, true);
}
