// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_ScreenplayMcpRunner.given;

public class an_embedded_connection : Specification
{
    protected JsonElement[] _responses;
    string _root;

    void Establish() => _root = AiProjectPaths.PhysicalRoot(Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"cratis-mcp-{Guid.NewGuid():N}")).FullName);

    protected void Exchange(string capabilities, params string[] requests)
    {
        var initialize = "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"protocolVersion\":\"2025-06-18\",\"capabilities\":" + capabilities + ",\"clientInfo\":{\"name\":\"cli-specs\",\"version\":\"1.0.0\"}}}";
        using var input = new StringReader(string.Join('\n', new[]
        {
            initialize,
            "{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\"}",
            "{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"tools/list\"}"
        }.Concat(requests)) + "\n");
        using var output = new StringWriter();
        new ScreenplayMcpRunner().Run(_root, input, output);
        _responses = [.. output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line =>
        {
            using var document = JsonDocument.Parse(line);
            return document.RootElement.Clone();
        })];
    }

    void Destroy() => Directory.Delete(_root, recursive: true);
}
