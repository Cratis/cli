// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Compression;
using System.Text;
using Cratis.Templating.Packages;

namespace Cratis.Templating.Specs.for_TemplatePackageStore.when_acquiring_a_package;

public class and_the_package_is_already_cached : given_a_store
{
    string _first = null!;
    string _second = null!;

    async Task Because()
    {
        _first = await Store.Acquire(Feed, "Test.Pkg", "1.1.0");

        // Changing the feed content afterwards must not affect the cached copy.
        File.Delete(Path.Combine(FeedRoot, "Test.Pkg.1.1.0.nupkg"));
        _second = await Store.Acquire(Feed, "Test.Pkg", "1.1.0");
    }

    [Fact] void should_resolve_from_the_cache_offline() => _second.ShouldEqual(_first);

    [Fact] void should_not_re_extract() => Directory.Exists(Path.Combine(StoreRoot, "Test.Pkg", "1.1.0")).ShouldBeTrue();
}
