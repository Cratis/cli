// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ProjectRestoreState.when_describing_what_was_not_restored;

public class and_one_project_was_not : Specification
{
    string _result;

    void Because() => _result = ProjectRestoreState.MessageFor(["MyApp"]);

    [Fact] void should_name_the_project() => _result.ShouldContain("'MyApp' has not been restored");
    [Fact] void should_say_what_to_do_about_it() => _result.ShouldContain("dotnet restore");
}
