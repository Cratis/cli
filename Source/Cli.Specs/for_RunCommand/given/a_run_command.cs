// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.given;

/// <summary>
/// Runs the command through the CLI against real temporary inputs, with the only <c language="csharp">docker</c> on the
/// path being one that records the arguments it is started with and then fails, as a container that stops before it
/// is ready would.
/// </summary>
/// <remarks>
/// The recording <c language="csharp">docker</c> is a shell script, so it is only installed on Linux and macOS. The specs
/// that need Docker to be reached use <see cref="Unix.FactAttribute"/>; the ones that assert it is never reached hold on
/// every platform.
/// </remarks>
public class a_run_command : Specification
{
    protected string _folder;
    protected StringWriter _error;

    string _root;
    string _dockerArguments;
    string _previousDirectory;
    string? _previousPath;
    TextWriter _previousError;

    /// <summary>
    /// Gets the arguments Docker was started with, one per line and in order, across every time it was started.
    /// </summary>
    protected IReadOnlyList<string> DockerArguments => File.Exists(_dockerArguments) ? File.ReadAllLines(_dockerArguments) : [];

    void Establish()
    {
        _previousDirectory = Directory.GetCurrentDirectory();
        _previousPath = Environment.GetEnvironmentVariable("PATH");
        _previousError = Console.Error;

        _root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())).FullName;
        var tools = Directory.CreateDirectory(Path.Combine(_root, "tools")).FullName;
        Directory.SetCurrentDirectory(Directory.CreateDirectory(Path.Combine(_root, "models")).FullName);

        // Read the folder back so specs compare against the same resolved path the command sees - the temp folder
        // is reached through a symbolic link on macOS.
        _folder = Directory.GetCurrentDirectory();
        _dockerArguments = Path.Combine(tools, "docker-arguments");

        if (!OperatingSystem.IsWindows())
        {
            var docker = Path.Combine(tools, "docker");
            File.WriteAllText(docker, $"#!/bin/sh\nprintf '%s\\n' \"$@\" >> '{_dockerArguments}'\nexit 1\n");
            File.SetUnixFileMode(docker, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        Environment.SetEnvironmentVariable("PATH", tools);
        _error = new StringWriter();
        Console.SetError(_error);
    }

    /// <summary>
    /// Runs <c language="csharp">run</c> through the CLI, the way it is invoked from a terminal.
    /// </summary>
    /// <param name="arguments">The arguments following the command name.</param>
    /// <returns>The exit code.</returns>
    protected Task<int> Run(params string[] arguments) =>
        CliApp.Create().RunAsync(["run", .. arguments, "--output", OutputFormats.JsonCompact]);

    void Destroy()
    {
        Console.SetError(_previousError);
        Environment.SetEnvironmentVariable("PATH", _previousPath);
        Directory.SetCurrentDirectory(_previousDirectory);
        _error.Dispose();

        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }

    /// <summary>
    /// Facts that need the recording <c language="csharp">docker</c>, which is only installed on Linux and macOS.
    /// </summary>
    public static class Unix
    {
        /// <summary>
        /// A fact that is skipped on Windows.
        /// </summary>
        public sealed class FactAttribute : Xunit.FactAttribute
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="FactAttribute"/> class.
            /// </summary>
            public FactAttribute()
            {
                if (OperatingSystem.IsWindows())
                {
                    Skip = "Requires a shell script to stand in for Docker.";
                }
            }
        }
    }
}
