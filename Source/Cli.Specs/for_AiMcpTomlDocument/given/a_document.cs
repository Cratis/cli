// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpTomlDocument.given;

public class a_document : Specification
{
    protected const string Original = "# user's header\r\nmodel = 'my-model'\r\n";
    protected const string Owned = "[mcp_servers.screenplay]\r\n  command = 'old'\r\n  args = ['screenplay', 'mcp']\r\n";
    protected const string Following = "# user's following table\r\n[mcp_servers.other]\r\ncommand = 'keep'\r\n";
    protected string _project;
    private protected AiMcpTomlDocument _document;

    void Establish()
    {
        _project = Path.Combine(Path.GetTempPath(), $"cratis-mcp-formatting-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_project);
        File.WriteAllText(Path.Combine(_project, "config.toml"), Original + Owned + Following);
        _document = new(_project, "config.toml");
    }

    protected string Content() => File.ReadAllText(Path.Combine(_project, "config.toml"));
    void Destroy() => Directory.Delete(_project, recursive: true);
}
