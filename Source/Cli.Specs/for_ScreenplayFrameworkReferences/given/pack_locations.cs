// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayFrameworkReferences.given;

public class pack_locations : Specification
{
    protected string _root;
    protected string _configuredRoot;
    protected string _pathRoot;
    protected string _runtimeRoot;
    protected string _runtimeDirectory;
    protected string _home;
    protected string _executable;
    protected IEnumerable<string> _result;

    void Establish()
    {
        _root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"screenplay-packs-{Guid.NewGuid():N}")).FullName;
        _configuredRoot = Path.Combine(_root, "configured");
        _pathRoot = Directory.CreateDirectory(Path.Combine(_root, "sdk")).FullName;
        _runtimeRoot = Path.Combine(_root, "bundled");
        _runtimeDirectory = Path.Combine(_runtimeRoot, "shared", "Microsoft.NETCore.App", "10.0.0");
        _home = Path.Combine(_root, "home");
        _executable = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";
        File.WriteAllText(Path.Combine(_pathRoot, _executable), string.Empty);
    }

    void Destroy() => Directory.Delete(_root, recursive: true);
}
