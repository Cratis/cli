// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpDocument;

public class when_setting_a_server_in_a_tab_indented_file : for_AiJsonMemberEditor.given.a_server_registration
{
    string _project;
    string _path;
    AiMcpDocument _document;

    void Establish()
    {
        _project = Path.Combine(Path.GetTempPath(), $"cratis-mcp-json-formatting-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(_project, ".vscode"));
        _path = Path.Combine(_project, ".vscode/mcp.json");
        File.WriteAllText(_path, Original);
        _document = new(_project, ".vscode/mcp.json");
    }

    void Because()
    {
        _document.Set("servers", "screenplay", _value);
        _document.Apply(AiFileOperations.Performing);
    }

    [Fact] void should_persist_the_exact_expected_bytes() => File.ReadAllText(_path).ShouldEqual(Expected);
    void Destroy() => Directory.Delete(_project, recursive: true);
}
