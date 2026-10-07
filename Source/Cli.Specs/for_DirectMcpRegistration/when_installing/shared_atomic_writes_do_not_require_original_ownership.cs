// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Ai;

namespace Cratis.Cli.for_DirectMcpRegistration.when_installing;

public class shared_atomic_writes_do_not_require_original_ownership : given.a_configuration_with_another_group
{
    void Because() => AiFileOperations.Performing.WriteAllTextAtomically(_path, "{\"shared\":\"written\"}\n");

    [given.unix_with_multiple_groups.Fact] void should_allow_the_shared_write() => File.ReadAllText(_path).ShouldContain("written");
    [given.unix_with_multiple_groups.Fact] void should_keep_the_shared_writers_original_rename_policy()
    {
        if (OperatingSystem.IsWindows()) return;
        using var handle = File.OpenHandle(_path);
        AiUnixFileOwnership.Read(handle).Group.ShouldEqual(_creationGroup);
    }
}
