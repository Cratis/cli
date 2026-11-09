// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// The exception that is thrown when a build kit cannot be used.
/// </summary>
/// <param name="problems">Each problem, starting with where it is: a JSON pointer into the manifest, or <c language="csharp">(document)</c>.</param>
public sealed class InvalidBuildKit(IReadOnlyList<string> problems) : Exception(string.Join(Environment.NewLine, problems))
{
    /// <summary>
    /// Gets each problem with the kit.
    /// </summary>
    public IReadOnlyList<string> Problems { get; } = problems;
}
