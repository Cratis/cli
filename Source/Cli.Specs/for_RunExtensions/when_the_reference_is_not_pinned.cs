// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Run.for_RunExtensions;

public class when_the_reference_is_not_pinned : given.an_admission
{
    void Establish()
    {
        Restore();
        File.Delete(Project.LockPath);
    }

    Task Because() => Admit(Project.Model, true);

    [Fact] void should_refuse_to_start() => Admitted.ExitCode.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_not_resolve_a_range_by_itself() => Output.ShouldContain("never resolves a range");
    [Fact] void should_leave_the_lock_missing() => File.Exists(Project.LockPath).ShouldBeFalse();
}
