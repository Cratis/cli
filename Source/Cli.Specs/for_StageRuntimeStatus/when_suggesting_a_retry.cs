// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageRuntimeStatus;

public class when_suggesting_a_retry : Specification
{
    readonly StageRuntimeStatus _status = new("unsupported", "eventmodel", [new("Routing", "Register", "Routes run only on the semantic engine.")]);

    [Fact] void should_include_the_original_path() => _status.SuggestionFor(new() { Path = "./models" }).ShouldEqual("Run again on the semantic engine: cratis run ./models --engine semantic");
    [Fact] void should_quote_a_path_with_spaces() => _status.SuggestionFor(new() { Path = "./my models" }).ShouldEqual("Run again on the semantic engine: cratis run \"./my models\" --engine semantic");
    [Fact] void should_use_the_current_folder_for_an_omitted_path() => _status.SuggestionFor(new()).ShouldContain(Directory.GetCurrentDirectory());

    [Fact]
    void should_quote_backslashes_for_the_current_shell()
    {
        var expected = OperatingSystem.IsWindows() ? "cratis run \"C:\\my models\" --engine semantic" : "cratis run \"C:\\\\my models\" --engine semantic";
        _status.SuggestionFor(new() { Path = @"C:\my models" }).ShouldEqual($"Run again on the semantic engine: {expected}");
    }

    [Fact]
    void should_preserve_non_default_options() => _status.SuggestionFor(new()
    {
        Path = "./models",
        Port = 9191,
        WorkbenchPort = 35001,
        Tag = "custom",
        PackageSet = "./approved packages.json",
        Packages = "./node_modules",
        Verbose = true
    }).ShouldEqual("Run again on the semantic engine: cratis run ./models --engine semantic --port 9191 --workbench-port 35001 --tag custom --package-set \"./approved packages.json\" --packages ./node_modules --verbose");

    [Fact] void should_still_suggest_semantic_for_mixed_refusals() => (_status with { Issues = [.. _status.Issues!, new("World", "model", "History cannot be reconstructed.")] }).SuggestionFor(new() { Path = "./models" }).ShouldContain("cratis run ./models --engine semantic");
    [Fact] void should_keep_the_generic_suggestion_for_unrelated_refusals() => (_status with { Issues = [new("World", "model", "History cannot be reconstructed.")] }).Suggestion.ShouldNotContain("--engine semantic");
    [Fact] void should_not_suggest_switching_semantic_to_itself() => (_status with { Engine = "semantic" }).Suggestion.ShouldNotContain("--engine semantic");
}
