// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ViewTarget.when_resolving;

public class with_a_missing_path : given.a_temporary_folder
{
    void Because() => _result = ViewTarget.Resolve(Path.Combine(_folder, "Missing"));

    [Fact] void should_not_resolve_a_project() => _result.ProjectFile.ShouldBeNull();
    [Fact] void should_say_it_does_not_exist() => _result.Error!.ShouldContain("does not exist");
}
