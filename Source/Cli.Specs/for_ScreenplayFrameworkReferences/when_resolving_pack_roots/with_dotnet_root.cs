// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayFrameworkReferences.when_resolving_pack_roots;

public class with_dotnet_root : given.pack_locations
{
    void Because() => _result = ScreenplayFrameworkReferences.PackRoots(_configuredRoot, _pathRoot, _runtimeDirectory, _home);

    [Fact] void should_prefer_the_configured_root_over_path_and_the_bundled_runtime() => _result.ShouldEqual([Path.Combine(_configuredRoot, "packs"), Path.Combine(_pathRoot, "packs"), Path.Combine(_runtimeRoot, "packs"), Path.Combine(_home, ".nuget", "packages")]);
}
