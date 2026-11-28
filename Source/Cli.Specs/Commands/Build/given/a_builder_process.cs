// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.given;

public class a_builder_process : Specification
{
    protected const string Check = "test -z \"${cRaTiS_bUiLdEr_ToKeN+x}${CrAtIs_StUdIo_McP_SeCrEt+x}\" && test \"$CRATIS_SPEC_CONTROL\" = present";
    protected readonly string Folder = Directory.CreateTempSubdirectory().FullName;
    protected readonly Dictionary<string, string> Overrides = new() { ["cRaTiS_bUiLdEr_ToKeN"] = "dummy", ["CrAtIs_StUdIo_McP_SeCrEt"] = "dummy" };
    readonly Dictionary<string, string?> _previous = [];

    void Establish()
    {
        foreach (var (key, value) in Overrides) Set(key, value);
        Set("CRATIS_SPEC_CONTROL", "present");
    }

    protected void Set(string key, string? value)
    {
        _previous.TryAdd(key, Environment.GetEnvironmentVariable(key));
        Environment.SetEnvironmentVariable(key, value);
    }

    protected string Script(string name, string body)
    {
        var path = Path.Combine(Folder, name);
        File.WriteAllText(path, "#!/bin/sh\n" + body + "\n");
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        return path;
    }

    void Destroy()
    {
        foreach (var (key, value) in _previous) Environment.SetEnvironmentVariable(key, value);
        Directory.Delete(Folder, true);
    }
}
