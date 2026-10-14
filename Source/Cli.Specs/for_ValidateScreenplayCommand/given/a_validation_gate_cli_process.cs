// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_ValidateScreenplayCommand.given;

public class a_validation_gate_cli_process : a_completeness_cli_process
{
    protected string _root;
    protected string _gatePath;
    protected JsonElement Summary => JsonSerializer.Deserialize<JsonElement>(_output);
    protected JsonElement Gate => Summary.GetProperty("gate");

    void Establish()
    {
        _root = Path.GetDirectoryName(_document)!;
        _gatePath = Path.Combine(_root, "screenplay.json");
    }

    protected void Declare(string json) => File.WriteAllText(_gatePath, json);
    protected string? SourceOf(string name) => Gate.GetProperty(name).GetProperty("source").GetString();
}
