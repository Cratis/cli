// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_PlayFileTargetResolver.when_resolving;

public class and_no_path_is_given : given.a_temporary_folder
{
    ScreenplayTarget _result;

    void Because() => _result = PlayFileTargetResolver.Resolve(null, _folder);

    [Fact] void should_resolve_the_current_directory() => _result.Path.ShouldEqual(_folder);
}
