// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Cli.for_ScreenplayDirectProjectWorkspaceBoundary.when_resolving;

public class with_a_safe_non_git_common_ancestor : Specification
{
    string _root;
    AdhocWorkspace _workspace;
    string _result;

    void Establish()
    {
        _root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"screenplay-direct-common-{Guid.NewGuid():N}")).FullName;
        _workspace = new AdhocWorkspace();
    }

    void Because()
    {
        var applicationPath = ProjectPath("Host", "Application.csproj");
        var dependencyPath = ProjectPath("Shared", "Dependency.csproj");
        var application = AddProject("Application", applicationPath);
        var dependency = AddProject("Dependency", dependencyPath);

        _result = ScreenplayDirectProjectWorkspaceBoundary.Resolve(applicationPath, [application, dependency]);
    }

    [Fact] void should_use_the_canonical_common_project_directory_ancestor() => _result.ShouldEqual(ScreenplayProjectSources.CanonicalPathOf(_root));
    [Fact] void should_not_broaden_to_the_target_project_directory() => _result.ShouldNotEqual(Path.Combine(_root, "Host"));

    void Destroy()
    {
        _workspace.Dispose();
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }

    string ProjectPath(string directory, string fileName)
    {
        var path = Path.Combine(_root, directory, fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, string.Empty);
        return path;
    }

    Project AddProject(string name, string filePath) => _workspace.AddProject(ProjectInfo.Create(
        ProjectId.CreateNewId(),
        VersionStamp.Create(),
        name,
        name,
        LanguageNames.CSharp,
        filePath: filePath));
}
