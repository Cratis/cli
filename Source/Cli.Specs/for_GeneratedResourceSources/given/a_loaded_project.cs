// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Cli.for_GeneratedResourceSources.given;

/// <summary>
/// Base context adding a project that compiles into the intermediate output folder, standing in for the project an
/// MSBuild workspace hands over.
/// </summary>
public class a_loaded_project : an_intermediate_output_folder
{
    protected AdhocWorkspace _workspace;
    protected Project _project;

    void Establish()
    {
        _workspace = new AdhocWorkspace();
        _project = _workspace.AddProject(
            ProjectInfo
                .Create(ProjectId.CreateNewId(), VersionStamp.Create(), "MyApp", "MyApp", LanguageNames.CSharp)
                .WithCompilationOutputInfo(default(CompilationOutputInfo).WithAssemblyPath(_assemblyPath)));
    }

    void Destroy() => _workspace.Dispose();
}
