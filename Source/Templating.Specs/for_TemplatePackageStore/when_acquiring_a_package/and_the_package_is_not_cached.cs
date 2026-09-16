// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Compression;
using System.Text;
using Cratis.Templating.Packages;

namespace Cratis.Templating.Specs.for_TemplatePackageStore.when_acquiring_a_package;

public class and_the_package_is_not_cached : given_a_store
{
    string _acquired = null!;

    async Task Because() => _acquired = await Store.Acquire(Feed, "Test.Pkg", "1.0.0");

    [Fact] void should_extract_into_the_store() => Path.Combine(_acquired, ".template.config", "template.json").ShouldNotBeNull();

    [Fact] void should_mark_the_extraction_complete() => File.Exists(Path.Combine(_acquired, ".cratis-complete")).ShouldBeTrue();

    [Fact] void should_discover_the_template_inside() =>
        TemplatePackageStore.DiscoverTemplates(_acquired)[0].Manifest.ShortName.ShouldEqual("testpkg");
}
