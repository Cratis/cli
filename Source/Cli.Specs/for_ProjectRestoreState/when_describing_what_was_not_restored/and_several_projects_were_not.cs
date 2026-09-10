// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ProjectRestoreState.when_describing_what_was_not_restored;

public class and_several_projects_were_not : Specification
{
    string _result;

    void Because() => _result = ProjectRestoreState.MessageFor(["MyApp", "MyApp.Domain", "MyApp.Read"]);

    [Fact] void should_name_the_first_project() => _result.ShouldContain("'MyApp'");
    [Fact] void should_count_the_rest_rather_than_list_them() => _result.ShouldContain("and 2 more have not been restored");
    [Fact] void should_say_what_to_do_about_it() => _result.ShouldContain("dotnet restore");
}
