// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Ai;
using Cratis.Cli.Commands.Direct;
using Cratis.Cli.Commands.Screenplay;

namespace Cratis.Cli;

/// <summary>
/// Identifies the invocation and when interactive CLI output is appropriate.
/// </summary>
/// <param name="arguments">The normalized command-line arguments.</param>
internal sealed class CliInvocation(string[] arguments)
{
    internal string[] Arguments { get; } = arguments;

    internal bool IsProtocol { get; } = ScreenplayMcpInvocation.IsProtocolRun(arguments) || DirectMcpInvocation.IsProtocolRun(arguments);

    internal bool IsCompletion { get; } = arguments.Length > 0 && string.Equals(arguments[0], "_complete", StringComparison.OrdinalIgnoreCase);

    internal bool ShowsInteractiveContext() => Arguments.Length == 0 && HasInteractiveOutput();

    internal bool ShowsCommunityPointer() => Arguments.Length == 1 && !GlobalSettings.IsAiAgentEnvironment() && IsRootHelp();

    internal bool StartsAiUpdateCheck() => ChecksForUpdates() && AiUpdateCheck.AppliesTo(Arguments) && HasInteractiveOutput();

    internal bool ShowsUpdateHints() => ChecksForUpdates() && HasInteractiveOutput();

    static bool HasInteractiveOutput() => !Console.IsOutputRedirected && !GlobalSettings.IsAiAgentEnvironment();

    bool ChecksForUpdates() => !IsProtocol && !IsCompletion && !SkipsUpdateHint();

    bool SkipsUpdateHint() => Arguments.Length > 0 && IsUpdateCommand();

    bool IsUpdateCommand() =>
        string.Equals(Arguments[0], "update", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Arguments[0], "version", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Arguments[0], "release-notes", StringComparison.OrdinalIgnoreCase);

    bool IsRootHelp() =>
        string.Equals(Arguments[0], "--help", StringComparison.Ordinal) || string.Equals(Arguments[0], "-h", StringComparison.Ordinal);
}
