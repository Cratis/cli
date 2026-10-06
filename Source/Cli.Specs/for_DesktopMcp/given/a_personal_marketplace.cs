// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Compression;
using System.Text.Json.Nodes;

namespace Cratis.Cli.for_DesktopMcp.given;

public class a_personal_marketplace : Specification
{
    protected string _home;
    protected string _marketplace;
    protected string _artifact;
    private protected ChatGptDesktopMcp _client;
    private protected DesktopMcpPlatform _platform;
    protected string _result;
    protected Exception _error;

    void Establish()
    {
        _home = AiProjectPaths.PhysicalRoot(Directory.CreateTempSubdirectory("cratis-desktop-").FullName);
        _platform = new("osx", "arm64", _home, _home, _home);
        _marketplace = Path.Combine(_home, ".agents", "plugins", "marketplace.json");
        Directory.CreateDirectory(Path.GetDirectoryName(_marketplace)!);
        File.WriteAllText(_marketplace, "{\n  \"name\": \"my-marketplace\",\n  \"extra\": { \"keep\": true },\n  \"plugins\": [{ \"name\": \"foreign\", \"source\": { \"source\": \"local\", \"path\": \"./mine\" } }]\n}\n");
        _client = new(_platform);
        _artifact = Package("4.55.0");
    }

    protected string Package(string version)
    {
        var artifact = Path.Combine(_home, $"screenplay-{version}.zip");
        using var archive = ZipFile.Open(artifact, ZipArchiveMode.Create);
        Write(archive, "plugin.json", new JsonObject { ["name"] = "cratis-screenplay", ["version"] = version }.ToJsonString());
        Write(archive, "mcp.json", "{\"mcpServers\":{\"screenplay\":{\"type\":\"stdio\",\"command\":\"./server/Cratis.Screenplay.Tool\",\"args\":[\"mcp\",\"--create-root\",\"${PLUGIN_DATA}/model\"]}}}");
        Write(archive, "server/Cratis.Screenplay.Tool", "binary fixture");
        return artifact;
    }

    void Destroy() => Directory.Delete(_home, recursive: true);

    static void Write(ZipArchive archive, string path, string content)
    {
        using var writer = new StreamWriter(archive.CreateEntry(path).Open());
        writer.Write(content);
    }
}
