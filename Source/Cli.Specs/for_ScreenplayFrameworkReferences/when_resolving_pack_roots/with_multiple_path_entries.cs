// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayFrameworkReferences.when_resolving_pack_roots;

public class with_multiple_path_entries : given.pack_locations
{
    string _path;

    void Establish()
    {
        var later = Directory.CreateDirectory(Path.Combine(_root, "later-sdk")).FullName;
        File.WriteAllText(Path.Combine(later, _executable), string.Empty);
        _path = string.Join(Path.PathSeparator, Path.Combine(_root, "missing"), _pathRoot, later);
    }

    void Because() => _result = ScreenplayFrameworkReferences.PackRoots(" ", _path, _runtimeDirectory, _home);

    [Fact] void should_use_the_first_dotnet_found_on_path() => _result.ShouldEqual([Path.Combine(_pathRoot, "packs"), Path.Combine(_runtimeRoot, "packs"), Path.Combine(_home, ".nuget", "packages")]);
}
