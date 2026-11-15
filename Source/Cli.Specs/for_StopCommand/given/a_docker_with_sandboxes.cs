// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StopCommand.given;

/// <summary>
/// Puts a <c language="csharp">docker</c> on the path that lists the sandboxes it is given, records every call, and
/// reports nothing running once a stop was asked for.
/// </summary>
public class a_docker_with_sandboxes : Specification
{
    protected StringWriter _error;

    string _root;
    string _calls;
    string? _previousPath;
    TextWriter _previousError;

    /// <summary>
    /// Gets the arguments Docker was started with, one per line and in order.
    /// </summary>
    protected IReadOnlyList<string> DockerArguments => File.Exists(_calls) ? File.ReadAllLines(_calls) : [];

    /// <summary>
    /// Installs the fake Docker.
    /// </summary>
    /// <param name="sandboxes">The names it reports as running.</param>
    protected void Install(params string[] sandboxes)
    {
        if (OperatingSystem.IsWindows()) return;
        var stopped = Path.Combine(_root, "stopped");
        var listing = string.Concat(sandboxes.Select(name => $"printf '%s\\n' '{name}'; "));
        var docker = Path.Combine(_root, "docker");
        File.WriteAllText(docker,
            $"#!/bin/sh\nprintf '%s\\n' \"$@\" >> '{_calls}'\n" +
            $"case \"$1\" in\nstop) : > '{stopped}' ;;\n" +
            $"ps) if [ -e '{stopped}' ]; then exit 0; fi; case \"$*\" in *--format*) {listing}true ;; *--quiet*) printf '%s\\n' abc ;; esac ;;\nesac\n");
        File.SetUnixFileMode(docker, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    }

    /// <summary>
    /// Runs <c language="csharp">stop</c> through the CLI.
    /// </summary>
    /// <param name="arguments">The arguments following the command name.</param>
    /// <returns>The exit code.</returns>
    protected Task<int> Stop(params string[] arguments) =>
        CliApp.Create().RunAsync(["stop", .. arguments, "--output", OutputFormats.JsonCompact]);

    void Establish()
    {
        _previousPath = Environment.GetEnvironmentVariable("PATH");
        _previousError = Console.Error;
        _root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())).FullName;
        _calls = Path.Combine(_root, "docker-arguments");
        Environment.SetEnvironmentVariable("PATH", _root);
        _error = new StringWriter();
        Console.SetError(_error);
    }

    void Destroy()
    {
        Console.SetError(_previousError);
        Environment.SetEnvironmentVariable("PATH", _previousPath);
        _error.Dispose();
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    /// <summary>
    /// Facts that need the fake <c language="csharp">docker</c>, which is only installed on Linux and macOS.
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
            /// <param name="sourceFilePath">The source file containing the specification.</param>
            /// <param name="sourceLineNumber">The source line containing the specification.</param>
            public FactAttribute([System.Runtime.CompilerServices.CallerFilePath] string? sourceFilePath = null, [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = -1)
                : base(sourceFilePath, sourceLineNumber)
            {
                if (OperatingSystem.IsWindows())
                {
                    Skip = "Requires a shell script to stand in for Docker.";
                }
            }
        }
    }
}
