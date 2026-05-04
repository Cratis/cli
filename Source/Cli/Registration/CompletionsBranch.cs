// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable RCS1251, SA1502 // Marker type is intentionally empty

namespace Cratis.Cli.Registration;

/// <summary>
/// Shell completion script generation and installation.
/// </summary>
[CliBranch("completions", "Generate and install shell completion scripts for bash, zsh, fish, and powershell. Use 'install' to auto-configure the current shell, or print scripts manually with the shell name.")]
public static class CompletionsBranch { }
