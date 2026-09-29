// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_uninstalling;

public class with_dry_run : given.a_home_and_a_project
{
    string _claude;
    string _manifest;

    void Establish()
    {
        Install(DirectMcpScope.User, ["claude"]);
        _claude = File.ReadAllText(HomeFile(".claude.json"));
        _manifest = File.ReadAllText(HomeFile(DirectMcpManifest.RelativePath));
    }

    void Because() => _plan = Uninstall(DirectMcpScope.User, dryRun: true);

    [Fact] void should_report_the_removal() => _plan.Changes.Single().Action.ShouldEqual("remove");
    [Fact] void should_show_the_value_being_removed() => _plan.Changes.Single().Value.ShouldContain("\"command\": \"cratis\"");
    [Fact] void should_leave_the_registration() => File.ReadAllText(HomeFile(".claude.json")).ShouldEqual(_claude);
    [Fact] void should_leave_the_ownership_record() => File.ReadAllText(HomeFile(DirectMcpManifest.RelativePath)).ShouldEqual(_manifest);
}
