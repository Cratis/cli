// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Compression;
using System.Text;
using Cratis.Templating.Packages;

namespace Cratis.Templating.Specs.for_TemplatePackageStore.when_acquiring_a_package;

public class and_a_local_nupkg_is_installed : given_a_store
{
    string _installed = null!;

    void Because() => _installed = Store.InstallLocal(Path.Combine(FeedRoot, "Test.Pkg.1.0.0.nupkg"));

    [Fact] void should_extract_it_into_the_local_area_of_the_store() =>
        _installed.StartsWith(StoreRoot).ShouldBeTrue();

    [Fact] void should_discover_the_template_inside() =>
        TemplatePackageStore.DiscoverTemplates(_installed).Count.ShouldEqual(1);
}
