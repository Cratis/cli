// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_AiProfilesListCommand.when_writing;

public class as_json : given.captured_profiles_output
{
    JsonDocument _document = null!;

    void Because()
    {
        Write(OutputFormats.Json);
        _document = JsonDocument.Parse(_output);
    }

    void Destroy() => _document.Dispose();

    [Fact] void should_list_every_profile() => _document.RootElement.GetArrayLength().ShouldEqual(2);
    [Fact] void should_name_each_profile() => _document.RootElement.EnumerateArray().Select(profile => profile.GetProperty("id").GetString()).ShouldEqual(["cratis/documentation", "cratis/engineering/csharp"]);
    [Fact] void should_name_the_catalog_of_each_profile() => _document.RootElement.EnumerateArray().Select(profile => profile.GetProperty("catalog").GetString()).ShouldEqual(["public", "engineering"]);
    [Fact] void should_include_languages() => _document.RootElement[1].GetProperty("languages").EnumerateArray().Select(language => language.GetString()).ShouldEqual(["csharp"]);
    [Fact] void should_include_composed_profiles() => _document.RootElement[1].GetProperty("composes").EnumerateArray().Select(profile => profile.GetString()).ShouldEqual(["cratis/documentation"]);
    [Fact] void should_succeed() => _exitCode.ShouldEqual(ExitCodes.Success);
}
