// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Integration.Chronicle;

/// <summary>
/// A fact that is skipped unless the Chronicle server under test enforces the administrator policy.
/// </summary>
/// <remarks>
/// Chronicle only enforces the administrator policy from 19.115.0. Set <c lang="text">CRATIS_CHRONICLE_ADMIN_ENFORCED=true</c> when the
/// server image is that version or later. Remove this gate when the CLI moves to such a Chronicle version.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class AdministratorEnforcedFactAttribute : FactAttribute
{
    /// <summary>
    /// The environment variable that tells the specs the server enforces the administrator policy.
    /// </summary>
    public const string EnforcedVariable = "CRATIS_CHRONICLE_ADMIN_ENFORCED";

    /// <summary>
    /// Initializes a new instance of the <see cref="AdministratorEnforcedFactAttribute"/> class.
    /// </summary>
    /// <param name="sourceFilePath">The source file of the test method.</param>
    /// <param name="sourceLineNumber">The source line of the test method.</param>
    public AdministratorEnforcedFactAttribute([System.Runtime.CompilerServices.CallerFilePath] string? sourceFilePath = null, [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(EnforcedVariable), "true", StringComparison.OrdinalIgnoreCase))
        {
            Skip = $"The Chronicle server does not enforce the administrator policy ({EnforcedVariable} is not 'true')";
        }
    }
}
