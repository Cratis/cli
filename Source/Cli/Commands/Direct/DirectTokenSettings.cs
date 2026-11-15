// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;

namespace Cratis.Cli.Commands.Direct;

/// <summary>Direct commands that accept a personal access token instead of browser login.</summary>
public class DirectTokenSettings : DirectSettings
{
    /// <summary>Gets or sets the personal access token option.</summary>
    [CommandOption("--token [TOKEN]")]
    [Description("Use a personal access token; omit the value to read DIRECT_TOKEN or stdin, or use --token=- for stdin. Prefer stdin or environment over argv.")]
    public FlagValue<string> Token { get; set; } = new();
}
