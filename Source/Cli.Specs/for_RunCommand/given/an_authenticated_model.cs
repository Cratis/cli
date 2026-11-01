// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Environments;
using Cratis.Environments.Secrets;

namespace Cratis.Cli.for_RunCommand.given;

/// <summary>
/// A model that signs people in with GitHub, in a project of its own, so the environments found are the spec's and not those of the machine it runs on.
/// </summary>
public class an_authenticated_model : a_run_command
{
    public const string ClientSecret = "spec-client-secret-4d2b";

    protected ProjectEnvironments _project;
    string _previousPassphrase;
    string _previousEnvironment;

    void Establish()
    {
        Directory.CreateDirectory(Path.Combine(Directory.GetParent(_folder)!.FullName, ".git"));
        File.WriteAllText(Path.Combine(_folder, "invoicing.play"), "domain Invoicing\n\nauthentication\n  provider GitHub\n");
        _project = ProjectEnvironments.Discover(_folder);
        _previousPassphrase = Environment.GetEnvironmentVariable("CRATIS_PASSPHRASE");
        _previousEnvironment = Environment.GetEnvironmentVariable(ProjectEnvironments.EnvironmentVariable);
        Environment.SetEnvironmentVariable("CRATIS_PASSPHRASE", "spec passphrase");
        Environment.SetEnvironmentVariable(ProjectEnvironments.EnvironmentVariable, null);
    }

    void Destroy()
    {
        Environment.SetEnvironmentVariable("CRATIS_PASSPHRASE", _previousPassphrase);
        Environment.SetEnvironmentVariable(ProjectEnvironments.EnvironmentVariable, _previousEnvironment);
    }

    /// <summary>
    /// Creates the development environment with the keys the model needs.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task CreateEnvironment()
    {
        var environment = ProjectEnvironment.Create(_project, "development", null, PassphraseSources.Fixed("spec passphrase"));
        await environment.SetAsync("authentication.providers.GitHub.clientId", "github-client", secret: false);
        await environment.SetAsync("authentication.providers.GitHub.clientSecret", ClientSecret, secret: true);
    }

    /// <summary>
    /// Replaces the stand-in Docker with one that creates networks, removes them and fails to run a container, as an image that cannot start would.
    /// </summary>
    protected void DockerThatCannotRun() =>
        File.WriteAllText(
            Path.Combine(_tools, "docker"),
            $"#!/bin/sh\nprintf '%s\\n' \"$@\" >> '{_dockerArguments}'\ncase \"$1\" in\nrun) printf '%s\\n' 'ERROR: the image cannot start'; exit 1 ;;\nesac\nexit 0\n");
}
