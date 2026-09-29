// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.given;

public class a_home_and_a_project : Specification
{
    protected static readonly string[] _every = ["claude", "codex", "copilot", "cursor", "opencode"];
    protected string _home;
    protected string _project;
    protected Dictionary<string, string> _environment;
    protected string _platform = "linux";
    private protected DirectMcpRegistration _plan;

    void Establish()
    {
        _home = Path.Combine(Path.GetTempPath(), $"cratis-direct-home-{Guid.NewGuid():N}");
        _project = Path.Combine(Path.GetTempPath(), $"cratis-direct-project-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_home);
        Directory.CreateDirectory(_project);
        _environment = [];
    }

    private protected DirectMcpLocations Locations => new(_home, _project, _platform, name => _environment.GetValueOrDefault(name));

    private protected DirectMcpRegistration Install(DirectMcpScope scope, string[] clients, string? tenant = null, bool dryRun = false)
    {
        var plan = DirectMcpRegistration.Install(scope, Locations, clients, DirectMcpClients.Arguments(null, tenant));
        if (plan.Conflicts.Count == 0) plan.Apply(new(dryRun));
        return plan;
    }

    private protected DirectMcpRegistration Uninstall(DirectMcpScope scope, params string[] clients) => Uninstall(scope, false, clients);

    private protected DirectMcpRegistration Uninstall(DirectMcpScope scope, bool dryRun, params string[] clients)
    {
        var plan = DirectMcpRegistration.Uninstall(scope, Locations, clients);
        if (plan.Conflicts.Count == 0) plan.Apply(new(dryRun));
        return plan;
    }

    protected string HomeFile(string relative) => Path.Combine(_home, relative);
    protected string ProjectFile(string relative) => Path.Combine(_project, relative);
    protected static JsonObject ReadJson(string path) => JsonNode.Parse(File.ReadAllText(path))!.AsObject();

    protected static void Write(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    void Destroy()
    {
        Directory.Delete(_home, recursive: true);
        Directory.Delete(_project, recursive: true);
    }
}
