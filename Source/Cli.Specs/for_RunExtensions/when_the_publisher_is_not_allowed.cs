// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Run.for_RunExtensions;

public class when_the_publisher_is_not_allowed : given.an_admission
{
    void Establish() => Restore(allow: false);

    Task Because() => Admit(Project.Model, true);

    [Fact] void should_refuse_to_start() => Admitted.ExitCode.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_name_the_extension() => Output.ShouldContain("cratis/stripe");
    [Fact] void should_say_it_is_not_allowed() => Output.ShouldContain("not on the allow-list");
    [Fact] void should_say_nothing_was_started() => Output.ShouldContain("Nothing was started");
}
