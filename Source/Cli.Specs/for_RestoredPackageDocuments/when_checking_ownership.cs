// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RestoredPackageDocuments;

/// <summary>
/// Package directories come from <c language="csharp">package</c> libraries only - a project reference is a library too,
/// and its directory is authored source.
/// </summary>
public class when_checking_ownership : Specification
{
    string _root;
    string _packageDirectory;
    string _projectDirectory;
    RestoredPackageDocuments _documents;

    void Establish()
    {
        _root = Path.Combine(Path.GetTempPath(), $"restored-package-documents-{Guid.NewGuid():N}");
        var packages = Path.Combine(_root, "packages");
        _packageDirectory = Directory.CreateDirectory(Path.Combine(packages, "fixture.builditems", "1.0.0")).FullName;
        Directory.CreateDirectory(Path.Combine(packages, "fixture.builditems", "1.0.0-preview"));
        _projectDirectory = Directory.CreateDirectory(Path.Combine(packages, "referenced")).FullName;
        var assets = Path.Combine(_root, "project.assets.json");
        var packageFolder = packages.Replace("\\", "\\\\", StringComparison.Ordinal);
        File.WriteAllText(
            assets,
            string.Join(
                Environment.NewLine,
                [
                    "{",
                    $"  \"packageFolders\": {{ \"{packageFolder}\": {{}} }},",
                    "  \"libraries\": {",
                    "    \"Fixture.BuildItems/1.0.0\": { \"type\": \"package\", \"path\": \"fixture.builditems/1.0.0\" },",
                    "    \"Referenced/1.0.0\": { \"type\": \"project\", \"path\": \"referenced\" }",
                    "  },",
                    "  \"targets\": { \"net10.0\": {} }",
                    "}"
                ]));
        _documents = RestoredPackageDocuments.From(assets);
    }

    [Fact] void should_own_a_document_inside_a_package_directory() => _documents.Owns(Path.Combine(_packageDirectory, "build", "Entry.cs")).ShouldBeTrue();
    [Fact] void should_not_own_a_document_in_a_sibling_directory_sharing_the_prefix() => _documents.Owns(Path.Combine(_packageDirectory + "-preview", "build", "Entry.cs")).ShouldBeFalse();
    [Fact] void should_not_own_a_document_of_a_project_reference() => _documents.Owns(Path.Combine(_projectDirectory, "Order.cs")).ShouldBeFalse();
    [Fact] void should_not_own_an_application_document() => _documents.Owns(Path.Combine(_root, "Order.cs")).ShouldBeFalse();

    void Destroy()
    {
        if (!string.IsNullOrEmpty(_root) && Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
