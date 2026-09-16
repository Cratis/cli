// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Compression;
using System.Text;
using Cratis.Templating.Packages;

namespace Cratis.Templating.Specs.for_TemplatePackageStore.when_acquiring_a_package;

public class and_the_path_is_neither_folder_nor_nupkg : given_a_store
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => Store.InstallLocal(Path.Combine(FeedRoot, "missing.txt")));

    [Fact] void should_fail_with_an_acquisition_error() => _error.ShouldBeOfExactType<TemplatePackageAcquisitionError>();
}
