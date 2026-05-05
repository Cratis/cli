// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Completions;

/// <summary>
/// Installs shell completion configuration for a given shell.
/// </summary>
public static class ShellCompletionInstaller
{
    static readonly string[] _supportedShells = ["bash", "zsh", "fish", "powershell"];

    /// <summary>
    /// Gets the list of shells supported by the installer.
    /// </summary>
    public static IReadOnlyList<string> SupportedShells => _supportedShells;

    /// <summary>
    /// Attempts to detect the current shell.
    /// On Unix uses the <c>$SHELL</c> environment variable.
    /// On Windows checks <c>PSModulePath</c> to detect PowerShell.
    /// </summary>
    /// <returns>The shell name (e.g. <c>zsh</c>, <c>powershell</c>), or <see langword="null"/> if undetectable.</returns>
    public static string? DetectShell()
    {
        if (!OperatingSystem.IsWindows())
        {
            var shellPath = Environment.GetEnvironmentVariable("SHELL") ?? string.Empty;
            var shell = Path.GetFileName(shellPath).ToLowerInvariant();
            return string.IsNullOrWhiteSpace(shell) ? null : shell;
        }

        // On Windows, $SHELL is not set. Detect PowerShell via PSModulePath which is present
        // in both Windows PowerShell 5.x and PowerShell 7+ (pwsh).
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PSModulePath")))
        {
            return "powershell";
        }

        return null;
    }

    /// <summary>
    /// Returns whether the given shell name is supported.
    /// </summary>
    /// <param name="shell">The shell name to check.</param>
    /// <returns><see langword="true"/> if the shell is supported; otherwise <see langword="false"/>.</returns>
    public static bool IsSupported(string shell) =>
        Array.Exists(_supportedShells, s => s == shell);

    /// <summary>
    /// Returns whether completions are already installed for the specified shell.
    /// </summary>
    /// <param name="shell">The shell to check.</param>
    /// <returns><see langword="true"/> if the config file already contains a cratis completions line.</returns>
    public static bool IsInstalled(string shell)
    {
        var configFile = shell switch
        {
            "bash" => ResolveHome(".bashrc"),
            "zsh" => ResolveHome(".zshrc"),
            "fish" => ResolveHome(".config", "fish", "config.fish"),
            "powershell" => ResolvePowerShellProfile(),
            _ => null
        };

        if (configFile is null || !File.Exists(configFile))
        {
            return false;
        }

        return File.ReadAllText(configFile).Contains("cratis completions", StringComparison.Ordinal);
    }

    /// <summary>
    /// Returns whether completions are already installed for any supported shell.
    /// </summary>
    /// <returns><see langword="true"/> if any supported shell config already contains a cratis completions line.</returns>
    public static bool IsInstalledForAnyShell() =>
        Array.Exists(_supportedShells, IsInstalled);

    /// <summary>
    /// Installs shell completions for the specified shell by appending an eval line to the shell config file.
    /// </summary>
    /// <param name="shell">The shell to install completions for: <c>bash</c>, <c>zsh</c>, or <c>fish</c>.</param>
    /// <param name="force">When <see langword="true"/>, removes any existing completions line before re-adding it.</param>
    /// <returns>A list of human-readable action strings describing what was done.</returns>
    public static IReadOnlyList<string> Install(string shell, bool force = false) =>
        shell switch
        {
            "bash" => InstallEval(ResolveHome(".bashrc"), "eval \"$(cratis completions bash)\"", force),
            "zsh" => InstallEval(ResolveHome(".zshrc"), "eval \"$(cratis completions zsh)\"", force),
            "fish" => InstallEval(ResolveHome(".config", "fish", "config.fish"), "cratis completions fish | source", force),
            "powershell" => InstallEval(ResolvePowerShellProfile(), "Invoke-Expression (& cratis completions powershell | Out-String)", force, ". $PROFILE  (or open a new terminal)"),
            _ => [$"Unknown shell '{shell}' — skipped (supported: bash, zsh, fish, powershell)"]
        };

    static List<string> InstallEval(string configFile, string line, bool force = false, string? reloadHint = null)
    {
        var actions = new List<string>();

        if (File.Exists(configFile))
        {
            var existing = File.ReadAllText(configFile);
            if (existing.Contains("cratis completions", StringComparison.Ordinal))
            {
                if (!force)
                {
                    actions.Add($"Completions already configured in {configFile} — nothing to do.");
                    return actions;
                }

                var lines = File.ReadAllLines(configFile).ToList();
                lines.RemoveAll(l => l.Contains("cratis completions", StringComparison.Ordinal));
                File.WriteAllLines(configFile, lines);
                actions.Add($"Removed existing completions line from {configFile}");
            }
        }
        else
        {
            var dir = Path.GetDirectoryName(configFile);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        File.AppendAllText(configFile, $"\n{line}\n");

        actions.Add($"Added completions to {configFile}");
        actions.Add($"Reload with:  {reloadHint ?? $"source {configFile}"}");

        return actions;
    }

    static string ResolvePowerShellProfile()
    {
        if (!OperatingSystem.IsWindows())
        {
            // macOS/Linux: PowerShell Core (pwsh) stores its profile here
            return ResolveHome(".config", "powershell", "Microsoft.PowerShell_profile.ps1");
        }

        // Windows: distinguish pwsh (PowerShell 7+) from Windows PowerShell 5.x via PSHOME
        var psHome = Environment.GetEnvironmentVariable("PSHOME") ?? string.Empty;
        var folder = psHome.Contains("WindowsPowerShell", StringComparison.OrdinalIgnoreCase)
            ? "WindowsPowerShell"
            : "PowerShell";

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            folder,
            "Microsoft.PowerShell_profile.ps1");
    }

    static string ResolveHome(params string[] segments) =>
        Path.Combine(
            [Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), .. segments]);
}
