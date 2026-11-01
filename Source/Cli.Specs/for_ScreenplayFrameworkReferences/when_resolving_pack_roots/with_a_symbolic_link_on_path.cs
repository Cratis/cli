// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayFrameworkReferences.when_resolving_pack_roots;

public class with_a_symbolic_link_on_path : given.pack_locations
{
    string _bin;

    void Establish()
    {
        _bin = Directory.CreateDirectory(Path.Combine(_root, "bin")).FullName;
        var intermediate = Path.Combine(_root, _executable);
        File.CreateSymbolicLink(intermediate, Path.Combine(_pathRoot, _executable));
        File.CreateSymbolicLink(Path.Combine(_bin, _executable), intermediate);
    }

    void Because() => _result = ScreenplayFrameworkReferences.PackRoots(null, _bin, _runtimeDirectory, _home);

    [Fact] void should_follow_the_links_to_the_sdk_root() => _result.ShouldEqual([Path.Combine(_pathRoot, "packs"), Path.Combine(_runtimeRoot, "packs"), Path.Combine(_home, ".nuget", "packages")]);
}
