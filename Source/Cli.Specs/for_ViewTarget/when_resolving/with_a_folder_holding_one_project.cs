// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ViewTarget.when_resolving;

public class with_a_folder_holding_one_project : given.a_temporary_folder
{
    string _project;

    void Establish() => _project = Create("Library.csproj");

    void Because() => _result = ViewTarget.Resolve(_folder);

    [Fact] void should_resolve_the_project() => _result.ProjectFile.ShouldEqual(_project);
    [Fact] void should_report_nothing() => _result.Error.ShouldBeNull();
}
