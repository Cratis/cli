// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle;

/// <summary>
/// The source a resolved setting value came from.
/// </summary>
public enum SettingSource
{
    /// <summary>
    /// The value was passed explicitly as a command line option.
    /// </summary>
    Option,

    /// <summary>
    /// The value came from the active context in the configuration.
    /// </summary>
    Context,

    /// <summary>
    /// The value is the built-in default.
    /// </summary>
    Default
}
