// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security;
using System.Text;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// A launchd agent in the signed-in user's GUI session, so it reaches the login keychain the builder token is kept in.
/// </summary>
/// <param name="home">The user's home folder.</param>
/// <param name="userId">The user's numeric id, which names the launchd domain <c language="shell">gui/&lt;uid&gt;</c>.</param>
public sealed class LaunchdBuildService(string home, string userId) : IBuildServicePlatform
{
    /// <inheritdoc/>
    public string Kind => "launchd";

    /// <summary>
    /// Gets the launchd label of a service.
    /// </summary>
    /// <param name="name">The service's name.</param>
    /// <returns>The label.</returns>
    public static string Label(string name) => $"io.cratis.{name}";

    /// <inheritdoc/>
    public string UnitPath(string name) => Path.Combine(home, "Library", "LaunchAgents", $"{Label(name)}.plist");

    /// <inheritdoc/>
    public string Logs(string name) => Path.Combine(home, "Library", "Logs", "Cratis", $"{name}.log");

    /// <inheritdoc/>
    public string Render(BuildServiceDefinition definition)
    {
        var plist = new StringBuilder()
            .AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>")
            .AppendLine("<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">")
            .AppendLine("<plist version=\"1.0\">")
            .AppendLine("<dict>")
            .AppendLine($"  <key>Label</key><string>{Escape(Label(definition.Name))}</string>")
            .AppendLine("  <key>ProgramArguments</key>")
            .AppendLine("  <array>");
        foreach (var argument in (string[])[definition.Program, .. definition.Arguments])
        {
            plist.AppendLine($"    <string>{Escape(argument)}</string>");
        }

        var logs = Logs(definition.Name);
        return plist
            .AppendLine("  </array>")
            .AppendLine($"  <key>WorkingDirectory</key><string>{Escape(definition.Project)}</string>")
            .AppendLine("  <key>EnvironmentVariables</key>")
            .AppendLine($"  <dict><key>PATH</key><string>{Escape(definition.SearchPath)}</string></dict>")
            .AppendLine("  <key>RunAtLoad</key><true/>")
            .AppendLine("  <key>KeepAlive</key><true/>")
            .AppendLine("  <key>ThrottleInterval</key><integer>30</integer>")
            .AppendLine("  <key>ProcessType</key><string>Background</string>")
            .AppendLine($"  <key>StandardOutPath</key><string>{Escape(logs)}</string>")
            .AppendLine($"  <key>StandardErrorPath</key><string>{Escape(logs)}</string>")
            .AppendLine("</dict>")
            .AppendLine("</plist>")
            .ToString();
    }

    /// <inheritdoc/>
    public IReadOnlyList<BuildServiceStep> Start(string name) =>
    [
        new("launchctl", ["bootout", $"gui/{userId}/{Label(name)}"], MayFail: true),
        new("launchctl", ["bootstrap", $"gui/{userId}", UnitPath(name)])
    ];

    /// <inheritdoc/>
    public IReadOnlyList<BuildServiceStep> Stop(string name) => [new("launchctl", ["bootout", $"gui/{userId}/{Label(name)}"], MayFail: true)];

    /// <inheritdoc/>
    public IReadOnlyList<BuildServiceStep> Forget(string name) => [];

    /// <inheritdoc/>
    public BuildServiceStep Query(string name) => new("launchctl", ["print", $"gui/{userId}/{Label(name)}"], MayFail: true);

    /// <inheritdoc/>
    public bool IsRunning(BuildProcessResult result) =>
        result.ExitCode == 0 && result.Output.Split('\n').Any(line => line.Trim() == "state = running");

    static string Escape(string value) => SecurityElement.Escape(value);
}
