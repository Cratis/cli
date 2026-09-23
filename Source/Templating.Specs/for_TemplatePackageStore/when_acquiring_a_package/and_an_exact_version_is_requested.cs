// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Compression;
using System.Text;
using Cratis.Templating.Packages;

namespace Cratis.Templating.Specs.for_TemplatePackageStore.when_acquiring_a_package;

public class and_an_exact_version_is_requested : given_a_store
{
    string? _resolved;

    async Task Because() => _resolved = await Store.ResolveVersion(Feed, "Test.Pkg", "1.0.0");

    [Fact] void should_return_it_without_listing() => _resolved.ShouldEqual("1.0.0");
}
