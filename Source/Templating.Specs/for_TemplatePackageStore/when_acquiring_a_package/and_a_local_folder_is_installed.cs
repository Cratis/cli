// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Compression;
using System.Text;
using Cratis.Templating.Packages;

namespace Cratis.Templating.Specs.for_TemplatePackageStore.when_acquiring_a_package;

public class and_a_local_folder_is_installed : given_a_store
{
    string _installed = null!;

    void Because()
    {
        var folder = Directory.CreateDirectory(Path.Combine(FeedRoot, "unpacked")).FullName;
        Directory.CreateDirectory(Path.Combine(folder, ".template.config"));
        File.WriteAllText(Path.Combine(folder, ".template.config", "template.json"), "{ \"name\": \"Unpacked\", \"shortName\": \"unpacked\" }");
        _installed = Store.InstallLocal(folder);
    }

    [Fact] void should_use_the_folder_directly() => _installed.ShouldContain("unpacked");
}
