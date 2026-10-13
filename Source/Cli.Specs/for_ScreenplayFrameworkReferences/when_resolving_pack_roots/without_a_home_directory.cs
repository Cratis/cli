// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayFrameworkReferences.when_resolving_pack_roots;

public class without_a_home_directory : given.pack_locations
{
    void Because() => _result = ScreenplayFrameworkReferences.PackRoots(null, null, _runtimeDirectory, string.Empty);

    [Fact] void should_omit_the_user_cache() => _result.ShouldContainOnly(Path.Combine(_runtimeRoot, "packs"));
}
