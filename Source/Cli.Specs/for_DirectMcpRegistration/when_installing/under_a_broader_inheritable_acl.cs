// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_installing;

public class under_a_broader_inheritable_acl : given.a_configuration_under_an_inheritable_acl
{
    void Because() => _plan = Install(DirectMcpScope.User, ["claude"]);

    [given.unix_acl_tools.Fact] void should_exercise_a_directory_that_inherits_access_entries() => _inherited.ShouldBeTrue();
    [given.unix_acl_tools.Fact] void should_keep_the_original_live_acl() => given.unix_acl_tools.Entries(_path).ShouldEqual(_acl);
    [given.unix_acl_tools.Fact] void should_write_the_original_inode() => File.ReadAllText(_alias).ShouldContain("cratis-direct");
    [given.unix_acl_tools.Fact] void should_create_an_acl_free_private_backup() => ShouldHavePrivateBackup(Directory.GetFiles(_home, ".claude.json.*.bak").Single());
    [given.unix_acl_tools.Fact] void should_preserve_the_exact_backup_content() => File.ReadAllText(Directory.GetFiles(_home, ".claude.json.*.bak").Single()).ShouldEqual("{\"theme\":\"dark\"}\n");
}
