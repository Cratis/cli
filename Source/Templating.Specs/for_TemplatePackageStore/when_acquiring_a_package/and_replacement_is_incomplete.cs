// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Packages;

namespace Cratis.Templating.Specs.for_TemplatePackageStore.when_acquiring_a_package;

public class and_replacement_is_incomplete : given_a_store
{
    string _sentinel = null!;
    Exception? _error;

    void Establish()
    {
        var destination = Directory.CreateDirectory(Path.Combine(StoreRoot, "Test.Pkg", "1.0.0")).FullName;
        _sentinel = Path.Combine(destination, "original.txt");
        File.WriteAllText(_sentinel, "original");
        File.WriteAllText(Path.Combine(FeedRoot, "Test.Pkg.1.0.0.nupkg"), "not a package");
    }

    async Task Because() => _error = await Catch.Exception(() => Store.Acquire(Feed, "Test.Pkg", "1.0.0"));

    [Fact] void should_report_failure() => _error.ShouldNotBeNull();
    [Fact] void should_preserve_the_existing_directory() => File.ReadAllText(_sentinel).ShouldEqual("original");
    [Fact] void should_not_mark_it_complete() => File.Exists(Path.Combine(Path.GetDirectoryName(_sentinel), ".cratis-complete")).ShouldBeFalse();
}
