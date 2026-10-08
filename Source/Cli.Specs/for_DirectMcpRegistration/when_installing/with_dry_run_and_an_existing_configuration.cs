// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_installing;

public class with_dry_run_and_an_existing_configuration : given.a_home_and_a_project
{
    const string Original = "{\"theme\":\"dark\"}\n";

    void Establish() => Write(HomeFile(".claude.json"), Original);

    void Because() => _plan = Install(DirectMcpScope.User, ["claude"], dryRun: true);

    [Fact] void should_not_create_a_backup_or_lock_or_manifest() => Directory.GetFileSystemEntries(_home).ShouldContainOnly(HomeFile(".claude.json"));
    [Fact] void should_preserve_the_configuration() => File.ReadAllText(HomeFile(".claude.json")).ShouldEqual(Original);
}
