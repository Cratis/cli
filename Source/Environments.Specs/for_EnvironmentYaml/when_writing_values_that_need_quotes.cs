// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments.for_EnvironmentYaml;

public class when_writing_values_that_need_quotes : Specification
{
    EnvironmentDocument _document;
    string _yaml;

    void Establish()
    {
        _document = new EnvironmentDocument();
        _document.Config["reserved"] = ConfigEntry.Plain("TrUe");
        _document.Config["unsafe"] = ConfigEntry.Plain("value # comment");
        _document.Config["empty"] = ConfigEntry.Plain(string.Empty);
        _document.Config["safe"] = ConfigEntry.Plain("AZaz09_./:@+=-");
    }

    void Because() => _yaml = EnvironmentYaml.Write(_document);

    [Fact] void should_quote_reserved_words_case_insensitively() => _yaml.ShouldContain("reserved: \"TrUe\"");
    [Fact] void should_quote_unsafe_characters() => _yaml.ShouldContain("unsafe: \"value # comment\"");
    [Fact] void should_quote_empty_values() => _yaml.ShouldContain("empty: \"\"");
    [Fact] void should_leave_safe_characters_unquoted() => _yaml.ShouldContain("safe: AZaz09_./:@+=-");
}
