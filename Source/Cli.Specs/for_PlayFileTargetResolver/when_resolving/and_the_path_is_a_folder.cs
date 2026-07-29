// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_PlayFileTargetResolver.when_resolving;

public class and_the_path_is_a_folder : given.a_temporary_folder
{
    string _nested;
    ScreenplayTarget _result;

    void Establish() => _nested = Directory.CreateDirectory(Path.Combine(_folder, "plays")).FullName;

    void Because() => _result = PlayFileTargetResolver.Resolve("plays", _folder);

    [Fact] void should_resolve() => _result.IsResolved.ShouldBeTrue();
    [Fact] void should_resolve_the_folder_itself() => _result.Path.ShouldEqual(_nested);
}
