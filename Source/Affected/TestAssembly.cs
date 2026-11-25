// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected;

/// <summary>
/// A built test project, ready to be listed or run.
/// </summary>
/// <param name="Project">The repository relative path of the project file.</param>
/// <param name="AssemblyPath">The absolute path of the built test assembly.</param>
public record TestAssembly(string Project, string AssemblyPath);
