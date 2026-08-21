// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli;

/// <summary>
/// Centralizes runtime detection for supported AI coding agents and explicit non-interactive execution.
/// </summary>
public static class AiAgentEnvironment
{
    /// <summary>
    /// The environment variable that explicitly marks a process as non-interactive.
    /// </summary>
    public const string NonInteractiveEnvironmentVariable = "CRATIS_NONINTERACTIVE";

    /// <summary>
    /// Determines whether the process is running inside a supported AI coding agent.
    /// </summary>
    /// <returns>True when a supported AI coding agent is detected.</returns>
    public static bool IsDetected() => IsDetected(Environment.GetEnvironmentVariable);

    /// <summary>
    /// Determines whether non-interactive execution was explicitly requested.
    /// </summary>
    /// <returns>True when <c>CRATIS_NONINTERACTIVE</c> has a non-empty value.</returns>
    public static bool IsNonInteractiveRequested() =>
        IsNonInteractiveRequested(Environment.GetEnvironmentVariable);

    internal static bool IsNonInteractiveRequested(Func<string, string?> getEnvironmentVariable) =>
        HasValue(getEnvironmentVariable(NonInteractiveEnvironmentVariable));

    internal static bool IsDetected(Func<string, string?> getEnvironmentVariable) =>
        IsClaudeCode(getEnvironmentVariable) ||
        IsCursor(getEnvironmentVariable) ||
        IsWindsurf(getEnvironmentVariable) ||
        IsPi(getEnvironmentVariable);

    internal static bool IsClaudeCode(Func<string, string?> getEnvironmentVariable) =>
        HasValue(getEnvironmentVariable("CLAUDECODE")) ||
        HasValue(getEnvironmentVariable("CLAUDE_CODE_ENTRYPOINT"));

    internal static bool IsGitHubCopilot(Func<string, string?> getEnvironmentVariable) =>
        HasValue(getEnvironmentVariable("VSCODE_PID")) ||
        EqualsValue(getEnvironmentVariable("TERM_PROGRAM"), "vscode");

    internal static bool IsCursor(Func<string, string?> getEnvironmentVariable) =>
        HasValue(getEnvironmentVariable("CURSOR_TRACE_DIR")) ||
        EqualsValue(getEnvironmentVariable("TERM_PROGRAM"), "cursor");

    internal static bool IsWindsurf(Func<string, string?> getEnvironmentVariable) =>
        HasValue(getEnvironmentVariable("WINDSURF_SESSION_ID")) ||
        EqualsValue(getEnvironmentVariable("TERM_PROGRAM"), "windsurf");

    internal static bool IsPi(Func<string, string?> getEnvironmentVariable) =>
        HasValue(getEnvironmentVariable("PI_CODING_AGENT")) ||
        HasValue(getEnvironmentVariable("PI_SESSION_ID"));

    static bool HasValue(string? value) => !string.IsNullOrEmpty(value);

    static bool EqualsValue(string? value, string expected) =>
        string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);
}
