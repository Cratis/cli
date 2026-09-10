// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ProjectRestoreState.when_deciding_whether_a_project_is_restored;

public class and_neither_path_is_known : Specification
{
    bool _result;

    void Because() => _result = ProjectRestoreState.IsRestored(null, null);

    [Fact] void should_take_the_project_for_restored_rather_than_invent_a_failure() => _result.ShouldBeTrue();
}
