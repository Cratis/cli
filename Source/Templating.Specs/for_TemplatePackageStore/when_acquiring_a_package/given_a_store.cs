// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Compression;
using System.Text;
using Cratis.Templating.Packages;

namespace Cratis.Templating.Specs.for_TemplatePackageStore.when_acquiring_a_package;

public class given_a_store : Specification
{
    protected string StoreRoot = null!;
    protected TemplatePackageStore Store = null!;
    protected string FeedRoot = null!;
    protected NuGetFeed Feed = null!;

    void Establish()
    {
        StoreRoot = Path.Combine(Path.GetTempPath(), "cratis-specs-store", Guid.NewGuid().ToString("N"));
        Store = new TemplatePackageStore(StoreRoot);
        FeedRoot = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "cratis-specs-feed", Guid.NewGuid().ToString("N"))).FullName;
        Feed = new NuGetFeed("local", FeedRoot);
        PublishPackage("Test.Pkg", "1.0.0");
        PublishPackage("Test.Pkg", "1.1.0");
    }

    protected void PublishPackage(string id, string version)
    {
        using var stream = File.Create(Path.Combine(FeedRoot, $"{id}.{version}.nupkg"));
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);
        var manifest = archive.CreateEntry($"{id}/.template.config/template.json");
        using var writer = new StreamWriter(manifest.Open());
        writer.Write("{ \"name\": \"Test Pkg\", \"shortName\": \"testpkg\" }");
    }
}
