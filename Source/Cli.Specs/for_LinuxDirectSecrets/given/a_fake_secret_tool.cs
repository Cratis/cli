// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_LinuxDirectSecrets.given;

/// <summary>A shell script standing in for secret-tool that records its arguments and standard input.</summary>
public class a_fake_secret_tool : Specification
{
    protected string _folder = null!;
    protected string _tool = null!;

    protected IReadOnlyList<string> Arguments => File.Exists(Path.Combine(_folder, "args")) ? File.ReadAllLines(Path.Combine(_folder, "args")) : [];
    protected string Input => File.ReadAllText(Path.Combine(_folder, "stdin"));

    void Establish()
    {
        _folder = Path.Combine(Path.GetTempPath(), "cratis-secret-tool-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
        _tool = Path.Combine(_folder, "secret-tool");
    }

    protected void Behave(string body)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        File.WriteAllText(_tool, $"#!/bin/sh\nprintf '%s\\n' \"$@\" > '{_folder}/args'\ncat > '{_folder}/stdin'\n{body}\n");
        File.SetUnixFileMode(_tool, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    }

    void Destroy() => Directory.Delete(_folder, recursive: true);
}
