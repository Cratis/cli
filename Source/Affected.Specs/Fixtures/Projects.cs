// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected.Fixtures;

/// <summary>
/// Builds project definitions and graphs for specs without touching the file system.
/// </summary>
public static class Projects
{
    public static ProjectDefinition Project(
        string path,
        bool test = false,
        bool generator = false,
        string[]? references = null,
        string[]? analyzers = null,
        string[]? externals = null) =>
        new(path, references ?? [], analyzers ?? [], test, generator, externals ?? []);

    public static ProjectGraph Graph(params ProjectDefinition[] projects) => new(projects);

    /// <summary>
    /// A library and the test project that exercises it.
    /// </summary>
    public static ProjectGraph LibraryWithSpecs() => Graph(
        Project("Source/Foo/Foo.csproj"),
        Project("Source/Foo.Specs/Foo.Specs.csproj", test: true, references: ["Source/Foo/Foo.csproj"]));
}
