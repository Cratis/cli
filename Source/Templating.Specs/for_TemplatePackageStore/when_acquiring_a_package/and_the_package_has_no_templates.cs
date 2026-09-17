// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Compression;
using System.Text;
using Cratis.Templating.Packages;

namespace Cratis.Templating.Specs.for_TemplatePackageStore.when_acquiring_a_package;

public class and_the_package_has_no_templates : given_a_store
{
    string _emptyRoot = null!;
    Exception? _error;

    void Establish()
    {
        _emptyRoot = Directory.CreateDirectory(Path.Combine(FeedRoot, "empty-package")).FullName;
    }

    void Because() => _error = Catch.Exception(() => TemplatePackageStore.DiscoverTemplates(_emptyRoot));

    [Fact] void should_report_that_nothing_was_found() =>
        _error!.Message.ShouldContain("no templates found");
}
