// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_uninstalling;

public class under_a_broader_inheritable_acl : given.a_configuration_under_an_inheritable_acl
{
    void Establish() => Install(DirectMcpScope.User, ["claude"]);

    void Because() => _plan = Uninstall(DirectMcpScope.User);

    [given.unix_acl_tools.Fact] void should_keep_the_original_live_acl() => given.unix_acl_tools.Entries(_path).ShouldEqual(_acl);
    [given.unix_acl_tools.Fact] void should_keep_the_original_inode_and_truncate_the_old_tail() => File.ReadAllText(_alias).ShouldNotContain("cratis-direct");
    [given.unix_acl_tools.Fact] void should_make_every_backup_acl_free_and_private()
    {
        foreach (var backup in Directory.GetFiles(_home, ".claude.json.*.bak")) ShouldHavePrivateBackup(backup);
    }
    [given.unix_acl_tools.Fact] void should_keep_other_configuration() => File.ReadAllText(_path).ShouldContain("dark");
}
