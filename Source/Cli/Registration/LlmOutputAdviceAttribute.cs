// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Registration;

/// <summary>
/// Provides per-command output format guidance for AI agents in the LLM context descriptor.
/// </summary>
/// <param name="recommendedFormat">The recommended output format (e.g. "plain", "json").</param>
/// <param name="reason">The reason for the recommendation.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class LlmOutputAdviceAttribute(string recommendedFormat, string reason) : Attribute
{
    /// <summary>
    /// Gets the recommended output format (e.g. "plain", "json", "json-compact").
    /// </summary>
    public string RecommendedFormat { get; } = recommendedFormat;

    /// <summary>
    /// Gets the reason for the recommendation (e.g. "plain is ~25x smaller").
    /// </summary>
    public string Reason { get; } = reason;

    /// <summary>
    /// Gets or sets the command name this advice belongs to.
    /// Only needed when a class has multiple <see cref="CliCommandAttribute"/> registrations.
    /// </summary>
    public string? CommandName { get; init; }
}
