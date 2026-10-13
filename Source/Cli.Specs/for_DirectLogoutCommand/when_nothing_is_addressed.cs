// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectLogoutCommand;

public class when_nothing_is_addressed
{
    [Fact] void should_report_remaining_credentials_when_no_login_is_active() => DirectLogoutCommand.NothingToLogOutOf(false, true, 2).ShouldContain("2 stored Direct credential(s) remain");
    [Fact] void should_point_to_targeted_logout_when_no_login_is_active() => DirectLogoutCommand.NothingToLogOutOf(false, true, 2).ShouldContain("--all");
    [Fact] void should_say_not_logged_in_when_nothing_is_stored() => DirectLogoutCommand.NothingToLogOutOf(false, true, 0).ShouldEqual("Not logged in to Direct.");
    [Fact] void should_name_the_target_when_a_targeted_logout_finds_nothing() => DirectLogoutCommand.NothingToLogOutOf(false, false, 2).ShouldEqual("No stored Direct credential for this origin and tenant.");
    [Fact] void should_say_nothing_is_stored_for_all() => DirectLogoutCommand.NothingToLogOutOf(true, false, 0).ShouldEqual("No stored Direct credentials.");
}
