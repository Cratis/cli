// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Cli.Commands.Ai;

internal sealed class AiMcpFile
{
    readonly string _project;
    readonly string _relative;

    internal AiMcpFile(string project, string relative)
    {
        _project = project;
        _relative = relative;
        var path = AiProjectPaths.Within(project, relative);
        Original = File.Exists(path) ? ReadBytes(path) : null;
    }

    internal string? Original { get; }

    internal void Apply(string content, AiFileOperations operations)
    {
        if (operations.DryRun || string.Equals(content, Original, StringComparison.Ordinal)) return;
        ConfirmUnchanged();
        var path = AiProjectPaths.Within(_project, _relative);
        operations.CreateDirectoryFor(path);
        operations.WriteAllTextAtomically(path, content, ConfirmUnchanged);
    }

    static string ReadBytes(string path) => new UTF8Encoding(false, true).GetString(File.ReadAllBytes(path));

    void ConfirmUnchanged()
    {
        var path = AiProjectPaths.Within(_project, _relative);
        var current = File.Exists(path) ? ReadBytes(path) : null;
        if (!string.Equals(current, Original, StringComparison.Ordinal)) throw new AiMcpConfigurationInvalid($"{_relative} changed during MCP installation; retry after reviewing it.");
    }
}
