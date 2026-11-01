// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments.Integration;

/// <summary>
/// A fact that is skipped where no Docker daemon is available.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class DockerFactAttribute : FactAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DockerFactAttribute"/> class.
    /// </summary>
    /// <param name="sourceFilePath">The source file of the test method.</param>
    /// <param name="sourceLineNumber">The source line of the test method.</param>
    public DockerFactAttribute([System.Runtime.CompilerServices.CallerFilePath] string? sourceFilePath = null, [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
        if (!DockerAvailability.IsAvailable)
        {
            Skip = "Docker is not available";
        }
    }
}
