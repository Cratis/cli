// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.View;

/// <summary>
/// Exception that gets thrown when MSBuild cannot say what a project builds.
/// </summary>
/// <param name="projectFile">The project that could not be evaluated.</param>
/// <param name="reason">Why it could not be evaluated.</param>
public class ProjectCouldNotBeEvaluated(string projectFile, string reason)
    : Exception($"Could not evaluate '{projectFile}' - {reason}");
