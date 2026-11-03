// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Packages;

namespace Cratis.Templating.Specs.for_TemplatePackageStore.when_acquiring_a_package;

public class with_invalid_requests : given_a_store
{
    [Theory]
    [InlineData("../Test.Pkg", "1.0.0")]
    [InlineData("Test/Pkg", "1.0.0")]
    [InlineData("Test.Pkg", "1.0.0-Beta/next")]
    [InlineData("Test.Pkg", "1.0.0-Beta\\next")]
    [InlineData("Test.Pkg", "C:\\1.0.0")]
    [InlineData("Test.Pkg", "1.0.0%2fnext")]
    [InlineData("Test.Pkg", "")]
    async Task should_refuse_before_creating_the_store(string id, string version)
    {
        var error = await Catch.Exception(() => Store.Acquire(Feed, id, version));
        error.ShouldBeOfExactType<TemplatePackageAcquisitionError>();
        Directory.Exists(StoreRoot).ShouldBeFalse();
    }

    [Theory]
    [InlineData("1.0.0-Beta/next")]
    [InlineData("1.0.0+Build%2fnext")]
    async Task should_validate_exact_resolution(string version)
    {
        var error = await Catch.Exception(() => Store.ResolveVersion(Feed, "Test.Pkg", version));
        error.ShouldBeOfExactType<TemplatePackageAcquisitionError>();
        Directory.Exists(StoreRoot).ShouldBeFalse();
    }
}
