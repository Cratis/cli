// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Ai;

#pragma warning disable CA1416 // Facts run only on the platform their test attribute selects; Because returns early elsewhere.

namespace Cratis.Cli.for_DirectMcpRegistration.when_applying;

public class on_a_filesystem_without_posix_acl_inspection : given.a_home_and_a_project
{
    Exception? _backupError;
    Exception? _clientError;
    Exception? _noAclError;

    void Because()
    {
        if (OperatingSystem.IsWindows()) return;
        _backupError = Catch.Exception(() => AiUnixFileAcl.CheckLinuxResult(-1, 95, "backup"));
        _clientError = Catch.Exception(() => AiUnixFileAcl.CheckLinuxResult(-1, 95, "new client file"));
        _noAclError = Catch.Exception(() => AiUnixFileAcl.CheckLinuxResult(-1, 61, "backup"));
    }

    [given.unix_only.Fact] void should_refuse_uninspectable_acl_protection() => _backupError.ShouldBeOfExactType<IOException>();
    [given.unix_only.Fact] void should_report_the_errno_and_unsupported_inspection() => _backupError!.Message.Contains("errno 95: filesystem does not support POSIX ACL inspection", StringComparison.Ordinal).ShouldBeTrue();
    [given.unix_only.Fact] void should_distinguish_a_new_client_file_from_a_backup() => _clientError!.Message.Contains("new client file ACL protection", StringComparison.Ordinal).ShouldBeTrue();
    [given.unix_only.Fact] void should_give_an_actionable_remedy() => _clientError!.Message.Contains("Create the client configuration manually", StringComparison.Ordinal).ShouldBeTrue();
    [given.unix_only.Fact] void should_accept_an_absent_acl() => _noAclError.ShouldBeNull();
}
