// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Cli.for_AiMcpRegistration.when_installing;

public class with_native_jsonc : given.a_screenplay_corpus
{
    const string Unrelated = "\t\"other\" : { /* commas , and braces } */ \"command\":\"keep\", \"unicode\":\"blå\", },";
    const string Input = "\"inputs\" : [ /* secret prompt */ {\"id\":\"user\",}, ],";
    string _after;

    void Establish() => Write(".vscode/mcp.json", $"\uFEFF{{\r\n// user's header\r\n{Input}\r\n\"servers\" : {{\r\n{Unrelated}\r\n// user's footer\r\n}},\r\n}}\r\n");
    void Because()
    {
        _result = Install();
        _after = Encoding.UTF8.GetString(File.ReadAllBytes(ProjectFile(".vscode/mcp.json")));
    }

    [Fact] void should_preserve_unrelated_member_bytes() => _after.Contains(Unrelated, StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_preserve_input_bytes() => _after.Contains(Input, StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_preserve_comments() => _after.Contains("// user's footer\r\n", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_preserve_the_byte_order_mark() => _after.StartsWith('\uFEFF').ShouldBeTrue();
    [Fact] void should_produce_valid_jsonc_with_the_registered_server() => JsonNode.Parse(_after[1..], documentOptions: new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip })!["servers"]!["screenplay"]!["command"]!.GetValue<string>().ShouldEqual("cratis");
    [Fact] void should_have_no_conflicts() => _result.Conflicts.ShouldBeEmpty();
}
