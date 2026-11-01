// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Cratis.Cli.Commands.Ai;

namespace Cratis.Cli.for_DirectMcpRegistration;

public class when_selecting_linux_open_flags : Specification
{
    [Fact] void should_use_x64_no_follow_and_search_only_directory_flags() => AiConfigurationFile.LinuxOpenFlags(Architecture.X64, directory: true, writable: false).ShouldEqual(0x2b0000);
    [Fact] void should_use_arm64_no_follow_and_search_only_directory_flags() => AiConfigurationFile.LinuxOpenFlags(Architecture.Arm64, directory: true, writable: false).ShouldEqual(0x28c000);
    [Fact] void should_use_x64_no_follow_read_write_file_flags() => AiConfigurationFile.LinuxOpenFlags(Architecture.X64, directory: false, writable: true).ShouldEqual(0xa0802);
    [Fact] void should_use_arm64_no_follow_read_write_file_flags() => AiConfigurationFile.LinuxOpenFlags(Architecture.Arm64, directory: false, writable: true).ShouldEqual(0x88802);
    [Fact] void should_refuse_unknown_open_architectures() => Catch.Exception(() => AiConfigurationFile.LinuxOpenFlags(Architecture.X86, directory: false, writable: true)).ShouldBeOfExactType<IOException>();
    [Fact] void should_use_the_x64_statx_syscall() => AiConfigurationFile.LinuxStatxNumber(Architecture.X64).ShouldEqual(332);
    [Fact] void should_use_the_arm64_statx_syscall() => AiConfigurationFile.LinuxStatxNumber(Architecture.Arm64).ShouldEqual(291);
    [Fact] void should_refuse_unknown_identity_architectures() => Catch.Exception(() => AiConfigurationFile.LinuxStatxNumber(Architecture.X86)).ShouldBeOfExactType<IOException>();
}
