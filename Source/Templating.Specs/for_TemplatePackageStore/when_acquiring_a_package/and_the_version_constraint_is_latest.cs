// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Compression;
using System.Text;
using Cratis.Templating.Packages;

namespace Cratis.Templating.Specs.for_TemplatePackageStore.when_acquiring_a_package;

public class and_the_version_constraint_is_latest : given_a_store
{
    string? _resolved;

    async Task Because() => _resolved = await Store.ResolveVersion(Feed, "Test.Pkg", "*");

    [Fact] void should_pick_the_highest_local_version() => _resolved.ShouldEqual("1.1.0");
}
