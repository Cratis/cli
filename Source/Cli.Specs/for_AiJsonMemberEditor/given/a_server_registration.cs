// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Cli.for_AiJsonMemberEditor.given;

public class a_server_registration : Specification
{
    protected const string Original = "{\n\t\"servers\": {\n\t\t\"chronicle\": {\n\t\t\t\"type\": \"stdio\",\n\t\t\t\"command\": \"dotnet\",\n\t\t\t\"args\": [\n\t\t\t\t\"mcp\",\n\t\t\t\t\"start\"\n\t\t\t]\n\t\t}\n\t},\n\t\"inputs\": []\n}\n";
    protected const string Registration = "\t\t\"screenplay\": {\n\t\t\t\"type\": \"stdio\",\n\t\t\t\"command\": \"cratis\",\n\t\t\t\"args\": [\n\t\t\t\t\"screenplay\",\n\t\t\t\t\"mcp\",\n\t\t\t\t\"--project-root\",\n\t\t\t\t\"${workspaceFolder}\"\n\t\t\t]\n\t\t}";
    protected const string Expected = "{\n\t\"servers\": {\n\t\t\"chronicle\": {\n\t\t\t\"type\": \"stdio\",\n\t\t\t\"command\": \"dotnet\",\n\t\t\t\"args\": [\n\t\t\t\t\"mcp\",\n\t\t\t\t\"start\"\n\t\t\t]\n\t\t},\n" + Registration + "\n\t},\n\t\"inputs\": []\n}\n";
    protected JsonNode _value;
    protected string _result;

    void Establish() => _value = JsonNode.Parse("""{"type":"stdio","command":"cratis","args":["screenplay","mcp","--project-root","${workspaceFolder}"]}""")!;

    protected static string Format(string content, string unit, string newline = "\n") => content.Replace("\t", unit, StringComparison.Ordinal).Replace("\n", newline, StringComparison.Ordinal);
    protected bool HasTrailingWhitespace() => _result.Split('\n').Any(line => line.TrimEnd('\r').EndsWith(' ') || line.TrimEnd('\r').EndsWith('\t'));
    protected string Command() => JsonNode.Parse(_result.StartsWith('\uFEFF') ? _result[1..] : _result, documentOptions: new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip })!["servers"]!["screenplay"]!["command"]!.GetValue<string>();
}
