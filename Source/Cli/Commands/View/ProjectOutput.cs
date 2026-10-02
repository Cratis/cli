// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.View;

/// <summary>
/// Represents what a project builds - the assembly it names and where the build puts it.
/// </summary>
/// <param name="AssemblyName">The name of the assembly the project builds.</param>
/// <param name="RootNamespace">The root namespace of the project.</param>
/// <param name="TargetFramework">The target framework that was read.</param>
/// <param name="TargetPath">The full path of the assembly the build produces for that framework and configuration.</param>
public record ProjectOutput(string AssemblyName, string RootNamespace, string TargetFramework, string TargetPath)
{
    /// <summary>
    /// Gets a value indicating whether the project has been built - whether its output assembly exists.
    /// </summary>
    public bool IsBuilt => File.Exists(TargetPath);
}
