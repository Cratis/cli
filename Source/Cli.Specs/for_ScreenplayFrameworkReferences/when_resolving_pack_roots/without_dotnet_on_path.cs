// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayFrameworkReferences.when_resolving_pack_roots;

public class without_dotnet_on_path : given.pack_locations
{
    void Because() => _result = ScreenplayFrameworkReferences.PackRoots(null, Path.Combine(_root, "missing"), _runtimeDirectory, _home);

    [Fact] void should_fall_back_to_the_runtime_parent_chain() => _result.ShouldEqual([Path.Combine(_runtimeRoot, "packs"), Path.Combine(_home, ".nuget", "packages")]);
}
