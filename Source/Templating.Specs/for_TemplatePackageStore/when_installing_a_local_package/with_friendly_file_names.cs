// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Specs.for_TemplatePackageStore.when_acquiring_a_package;

namespace Cratis.Templating.Specs.for_TemplatePackageStore.when_installing_a_local_package;

public class with_friendly_file_names : given_a_store
{
    [Theory]
    [InlineData("templates (1).nupkg")]
    [InlineData("My Templates.nupkg")]
    [InlineData("Pkg.1.0.0+build.nupkg")]
    void should_install_a_package_with_a_single_safe_file_name(string name)
    {
        var package = Path.Combine(FeedRoot, name);
        File.Copy(Path.Combine(FeedRoot, "Test.Pkg.1.0.0.nupkg"), package);
        var installed = Store.InstallLocal(package);
        installed.ShouldEqual(Path.Combine(StoreRoot, "local", Path.GetFileNameWithoutExtension(name)));
        File.Exists(Path.Combine(installed, ".cratis-complete")).ShouldBeTrue();
    }

    [Theory]
    [InlineData("..nupkg")]
    [InlineData(".cratis-stage.nupkg")]
    void should_reject_a_reserved_or_relative_component(string name)
    {
        var package = Path.Combine(FeedRoot, name);
        File.Copy(Path.Combine(FeedRoot, "Test.Pkg.1.0.0.nupkg"), package);
        Catch.Exception(() => Store.InstallLocal(package)).ShouldBeOfExactType<TemplatePackageAcquisitionError>();
        File.Exists(package).ShouldBeTrue();
        Directory.Exists(Path.Combine(StoreRoot, "local")).ShouldBeFalse();
    }
}
