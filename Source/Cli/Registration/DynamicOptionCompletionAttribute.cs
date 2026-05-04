// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Registration;

/// <summary>
/// Annotates a command settings property to enable dynamic shell completion for its option value.
/// When placed on a property that also has <see cref="Spectre.Console.Cli.CommandOptionAttribute"/>,
/// the generated shell completion scripts will call <c>cratis _complete &lt;context&gt;</c> to
/// provide live candidates when the user presses Tab after the option flag.
/// </summary>
/// <param name="context">
/// The completion context key recognized by <c>cratis _complete</c>,
/// for example <c>"event-types"</c>, <c>"event-stores"</c>, or <c>"observers"</c>.
/// </param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class DynamicOptionCompletionAttribute(string context) : Attribute
{
    /// <summary>
    /// Gets the completion context key passed to <c>cratis _complete</c>,
    /// e.g. <c>"event-types"</c> or <c>"event-stores"</c>.
    /// </summary>
    public string Context { get; } = context;
}
