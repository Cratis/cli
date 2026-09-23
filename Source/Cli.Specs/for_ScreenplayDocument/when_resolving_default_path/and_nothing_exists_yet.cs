// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayDocument.when_resolving_default_path;

public class and_nothing_exists_yet : given.a_temporary_folder
{
    string _result;

    void Because() => _result = ScreenplayDocument.ResolveDefaultPath(_folder);

    [Fact] void should_resolve_to_screenplay_play() => _result.ShouldEqual(Path.Combine(_folder, "Screenplay.play"));
}
