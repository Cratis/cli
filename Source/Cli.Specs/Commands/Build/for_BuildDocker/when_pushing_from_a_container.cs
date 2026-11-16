// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuildDocker;

public class when_pushing_from_a_container : Specification
{
    IReadOnlyList<string> _withToken;
    IReadOnlyList<string> _withGhToken;
    IReadOnlyList<string> _withoutToken;

    void Because()
    {
        _withToken = BuildDocker.GitConfiguration(_ => true);
        _withGhToken = BuildDocker.GitConfiguration(name => name == "GH_TOKEN");
        _withoutToken = BuildDocker.GitConfiguration(_ => false);
    }

    [Fact] void should_trust_the_mounted_project() => _withoutToken.ShouldContain("GIT_CONFIG_VALUE_0=/workspace");
    [Fact] void should_add_no_credential_helper_without_a_token() => _withoutToken.ShouldContain("GIT_CONFIG_COUNT=1");
    [Fact] void should_sign_in_to_github_with_the_token_it_is_given() => _withToken.ShouldContain("GIT_CONFIG_KEY_1=credential.https://github.com.helper");
    [Fact] void should_count_both_settings() => _withToken.ShouldContain("GIT_CONFIG_COUNT=2");
    [Fact] void should_read_the_token_from_the_containers_environment() => _withToken.First(argument => argument.StartsWith("GIT_CONFIG_VALUE_1", StringComparison.Ordinal)).ShouldContain("password=$GITHUB_TOKEN");
    [Fact] void should_use_the_other_variable_when_it_is_the_only_one() => _withGhToken.First(argument => argument.StartsWith("GIT_CONFIG_VALUE_1", StringComparison.Ordinal)).ShouldContain("password=$GH_TOKEN");
    [Fact] void should_answer_only_when_git_asks_for_a_credential() => _withToken.First(argument => argument.StartsWith("GIT_CONFIG_VALUE_1", StringComparison.Ordinal)).ShouldContain("test \"$1\" = get");
}
