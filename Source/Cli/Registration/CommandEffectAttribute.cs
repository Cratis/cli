// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Registration;

/// <summary>
/// Declares the strongest state change a command can make. Required on every <see cref="CliCommandAttribute"/> class;
/// the source generator fails the build when it is missing.
/// </summary>
/// <remarks>
/// Classify by what the implementation does, not by the command's name. When a command can have different effects
/// depending on its target or options, declare the strongest one - options such as <c language="csharp">--dry-run</c> may only lower it.
/// Whether the command prompts for confirmation is not declared here: it is derived from the command overriding
/// <c language="csharp">ChronicleCommand.GetConfirmationPrompt</c> or calling <see cref="ConfirmationHelper"/>.
/// </remarks>
/// <param name="effect">The strongest <see cref="CommandEffect"/> the command can have.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class CommandEffectAttribute(CommandEffect effect) : Attribute
{
    /// <summary>
    /// Gets the strongest <see cref="CommandEffect"/> the command can have.
    /// </summary>
    public CommandEffect Effect { get; } = effect;
}
