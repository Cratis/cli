// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// A systemd user unit, started with the user's service manager and restarted when the build loop stops.
/// </summary>
/// <param name="configHome">The user's configuration folder: <c language="shell">$XDG_CONFIG_HOME</c>, or <c language="shell">~/.config</c>.</param>
public sealed class SystemdBuildService(string configHome) : IBuildServicePlatform
{
    /// <inheritdoc/>
    public string Kind => "systemd";

    /// <inheritdoc/>
    public string UnitPath(string name) => Path.Combine(configHome, "systemd", "user", $"{name}.service");

    /// <inheritdoc/>
    public string Logs(string name) => $"journalctl --user -u {name}.service";

    /// <inheritdoc/>
    public string Render(BuildServiceDefinition definition) => new StringBuilder()
        .AppendLine("[Unit]")
        .AppendLine($"Description=Cratis builder for {Specifiers(definition.Project)}")
        .AppendLine("Wants=network-online.target")
        .AppendLine("After=network-online.target")
        .AppendLine()
        .AppendLine("[Service]")
        .AppendLine("Type=simple")
        .AppendLine($"WorkingDirectory={Specifiers(definition.Project)}")
        .AppendLine($"Environment={Quoted(Specifiers($"PATH={definition.SearchPath}"))}")
        .AppendLine($"ExecStart={string.Join(' ', ((string[])[definition.Program, .. definition.Arguments]).Select(argument => Quoted(Specifiers(argument).Replace("$", "$$", StringComparison.Ordinal))))}")
        .AppendLine("Restart=always")
        .AppendLine("RestartSec=30")
        .AppendLine()
        .AppendLine("[Install]")
        .AppendLine("WantedBy=default.target")
        .ToString();

    /// <inheritdoc/>
    public IReadOnlyList<BuildServiceStep> Start(string name) =>
    [
        new("systemctl", ["--user", "daemon-reload"]),
        new("systemctl", ["--user", "enable", $"{name}.service"]),
        new("systemctl", ["--user", "restart", $"{name}.service"])
    ];

    /// <inheritdoc/>
    public IReadOnlyList<BuildServiceStep> Stop(string name) =>
        [new("systemctl", ["--user", "disable", "--now", $"{name}.service"], MayFail: true)];

    /// <inheritdoc/>
    public IReadOnlyList<BuildServiceStep> Forget(string name) => [new("systemctl", ["--user", "daemon-reload"], MayFail: true)];

    /// <inheritdoc/>
    public BuildServiceStep Query(string name) => new("systemctl", ["--user", "is-active", $"{name}.service"], MayFail: true);

    /// <inheritdoc/>
    public bool IsRunning(BuildProcessResult result) => result.ExitCode == 0 && result.Output.Trim() == "active";

    /// <summary>
    /// Keeps a value literal where systemd expands specifiers (<c language="shell">%</c>): every setting of a unit. A path is
    /// written as it is, never quoted; only <c language="shell">ExecStart</c> also expands <c language="shell">$</c> variables.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The value as systemd reads it literally.</returns>
    static string Specifiers(string value) => value.Replace("%", "%%", StringComparison.Ordinal);

    static string Quoted(string value) =>
        $"\"{value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
}
