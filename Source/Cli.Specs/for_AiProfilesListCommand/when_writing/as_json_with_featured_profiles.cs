// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_AiProfilesListCommand.when_writing;

public class as_json_with_featured_profiles : given.captured_featured_profiles_output
{
    JsonDocument _document = null!;

    void Because()
    {
        Write(OutputFormats.Json);
        _document = JsonDocument.Parse(_output);
    }

    void Destroy() => _document.Dispose();

    [Fact] void should_list_every_profile() => _document.RootElement.EnumerateArray().Select(profile => profile.GetProperty("id").GetString()).ShouldEqual(["cratis/application", "cratis/application/csharp"]);
    [Fact] void should_mark_which_profiles_are_featured() => _document.RootElement.EnumerateArray().Select(profile => profile.GetProperty("featured").GetBoolean()).ShouldEqual([true, false]);
    [Fact] void should_describe_the_featured_profile() => _document.RootElement[0].GetProperty("description").GetString().ShouldEqual("Build an application on Cratis.");
}
