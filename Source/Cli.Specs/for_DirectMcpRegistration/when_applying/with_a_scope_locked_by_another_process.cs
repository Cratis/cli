// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_applying;

public class with_a_scope_locked_by_another_process : given.a_home_and_a_project
{
    int _exitCode;
    string _output;
    string _errors;

    void Establish() => Install(DirectMcpScope.Project, ["claude"]);

    async Task Because()
    {
        // Windows resolves UserProfile through the native known-folder API, not a HOME override.
        var home = OperatingSystem.IsWindows() ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) : _home;
        using var held = DirectMcpManifest.AcquireLock(_project, home);
        var specs = GetType().Assembly.Location;
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = _project,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        if (!OperatingSystem.IsWindows()) start.Environment["HOME"] = _home;
        string[] arguments = ["exec", "--runtimeconfig", Path.ChangeExtension(specs, ".runtimeconfig.json"), "--depsfile", Path.ChangeExtension(specs, ".deps.json"), typeof(DirectMcpCommand).Assembly.Location,
            "direct", "mcp", "uninstall", "--scope", "project", "-o", "json-compact"];
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var child = new Process { StartInfo = start };
        child.Start().ShouldBeTrue();
        var output = child.StandardOutput.ReadToEndAsync();
        var errors = child.StandardError.ReadToEndAsync();
        try
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await child.WaitForExitAsync(deadline.Token);
            _exitCode = child.ExitCode;
            _output = await output;
            _errors = await errors;
        }
        finally
        {
            if (!child.HasExited) child.Kill(entireProcessTree: true);
            await child.WaitForExitAsync();
            await Task.WhenAll(output, errors);
        }
    }

    [Fact] void should_refuse_the_child_command() => _exitCode.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_explain_that_the_scope_is_busy() => (_output + _errors).ShouldContain("Another Direct MCP registration command");
    [Fact] void should_keep_the_manifest() => DirectMcpManifest.Read(_project).Servers.Count.ShouldEqual(1);
    [Fact] void should_keep_the_client_registration() => IsRegistered(ProjectFile(".mcp.json"), "mcpServers").ShouldBeTrue();
}
