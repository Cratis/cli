// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_installing;

public class with_restricted_permissions : given.a_restricted_configuration
{
    void Because() => _plan = Install(DirectMcpScope.User, ["claude"]);

    [Fact] void should_preserve_the_live_file_permissions() => ShouldRetainPermissions(_path);
    [Fact] void should_preserve_the_backup_permissions() => ShouldRetainPermissions(Directory.GetFiles(_home, ".claude.json.*.bak").Single());
    [Fact] void should_install_the_registration() => IsRegistered(_path, "mcpServers").ShouldBeTrue();
}
