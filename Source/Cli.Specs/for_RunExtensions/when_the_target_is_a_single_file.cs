// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Run.for_RunExtensions;

public class when_the_target_is_a_single_file : given.an_admission
{
    void Establish() => Restore();

    Task Because() => Admit(Project.Model, false);

    [Fact] void should_refuse_to_start() => Admitted.ExitCode.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_ask_for_the_folder() => Output.ShouldContain("Run the folder");
}
