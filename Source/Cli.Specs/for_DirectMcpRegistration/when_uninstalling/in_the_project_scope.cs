// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_uninstalling;

public class in_the_project_scope : given.a_home_and_a_project
{
    string _lock;

    void Establish()
    {
        Install(DirectMcpScope.Project, ["claude"]);
        _lock = Directory.GetFiles(HomeFile(".cratis/direct-mcp-locks")).Single();
    }

    void Because() => _plan = Uninstall(DirectMcpScope.Project);

    [Fact] void should_remove_the_ownership_record() => File.Exists(ProjectFile(DirectMcpManifest.RelativePath)).ShouldBeFalse();
    [Fact] void should_leave_no_lock_file_in_the_project() => Directory.GetFiles(ProjectFile(".cratis")).ShouldBeEmpty();
    [Fact] void should_reuse_the_existing_user_lock() => Directory.GetFiles(HomeFile(".cratis/direct-mcp-locks")).ShouldContainOnly(_lock);
}
